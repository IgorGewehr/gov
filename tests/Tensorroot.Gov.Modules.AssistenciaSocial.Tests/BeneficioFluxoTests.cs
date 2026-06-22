using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Beneficio"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Beneficio.rules.md, sobre SQLite em memoria com
/// auditoria e isolamento por tenant. O salario minimo e versionado por competencia (nunca hardcoded).
/// </summary>
public sealed class BeneficioFluxoTests : AssistenciaSocialTestBase
{
    private static readonly Guid Familia1 = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid Familia2 = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static readonly Competencia Junho2026 = Competencia.De(2026, 6);

    // Salario minimo vigente (parametrizado, nunca hardcoded no dominio) usado nos cenarios.
    private static readonly ValorMonetario SmJunho2026 = ValorMonetario.De(1412m);

    private static Beneficio Solicitar(TipoBeneficio tipo, Guid? familiaId = null, Competencia? competencia = null)
        => Beneficio.Solicitar(TenantA, familiaId ?? Familia1, tipo, competencia ?? Junho2026);

    private static RendaPerCapita Renda(decimal valor) => RendaPerCapita.Calcular(valor, 1);

    private static DadosElegibilidade Dados(
        int idade = 70,
        bool pcd = false,
        bool avaliacao = false,
        bool acumula = false,
        bool cadUnico = true)
        => new(idade, pcd, avaliacao, acumula, cadUnico, RendaFamiliar: 0m, MembrosFamilia: 1);

    private static readonly DateOnly Hoje = new(2026, 6, 21);

    // ---------- Invariantes ----------

    [Fact] // I-1 + B-11: criterio sem salario minimo vigente e rejeitado (nunca hardcoded).
    public void Invariante_1_criterio_sem_salario_minimo_e_rejeitado()
    {
        var acao = () => CriterioElegibilidade.Vigente(TipoBeneficio.Bpc, null!);

        acao.Should().Throw<ArgumentNullException>();
    }

    [Fact] // I-2 + Cenario 2: BPC concedido a idoso com renda < 1/4 SM.
    public void Invariante_2_bpc_concedido_a_idoso_elegivel()
    {
        var beneficio = Solicitar(TipoBeneficio.Bpc);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Bpc, SmJunho2026);

        // 1412/4 = 353; renda 300 < 353.
        var resultado = beneficio.AvaliarElegibilidade(criterio, Dados(idade: 70), Renda(300m), ValorMonetario.De(1412m), Hoje);

        resultado.Elegivel.Should().BeTrue();
        beneficio.Situacao.Should().Be(SituacaoBeneficio.Concedida);
        beneficio.DataDecisao.Should().Be(Hoje);
        beneficio.DomainEvents.OfType<BeneficioConcedido>().Should().ContainSingle();
    }

    [Fact] // I-2 + Cenario 1 + B-1: BPC indeferido por renda >= 1/4 SM.
    public void Invariante_2_bpc_indeferido_por_renda()
    {
        var beneficio = Solicitar(TipoBeneficio.Bpc);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Bpc, SmJunho2026);

        // B-1: exatamente 1/4 (353) => indeferido (regra estritamente < 1/4).
        var resultado = beneficio.AvaliarElegibilidade(criterio, Dados(idade: 70, pcd: true, avaliacao: true), Renda(353m), null, Hoje);

        resultado.Elegivel.Should().BeFalse();
        beneficio.Situacao.Should().Be(SituacaoBeneficio.Indeferida);
        beneficio.MotivoIndeferimento.Should().NotBeNullOrWhiteSpace();
        beneficio.DomainEvents.OfType<BeneficioIndeferido>().Should().ContainSingle();
    }

    [Fact] // I-3 + Cenario 3: BPC indeferido por acumulacao da Seguridade Social.
    public void Invariante_3_bpc_indeferido_por_acumulacao()
    {
        var beneficio = Solicitar(TipoBeneficio.Bpc);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Bpc, SmJunho2026);

        var resultado = beneficio.AvaliarElegibilidade(criterio, Dados(idade: 70, acumula: true), Renda(100m), null, Hoje);

        resultado.Elegivel.Should().BeFalse();
        beneficio.Situacao.Should().Be(SituacaoBeneficio.Indeferida);
        beneficio.MotivoIndeferimento.Should().Contain("Seguridade");
    }

    [Fact] // I-2 + B-3: PCD sem avaliacao biopsicossocial e indeferido.
    public void Invariante_2_pcd_sem_avaliacao_biopsicossocial_e_indeferido()
    {
        var beneficio = Solicitar(TipoBeneficio.Bpc);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Bpc, SmJunho2026);

        var resultado = beneficio.AvaliarElegibilidade(criterio, Dados(idade: 40, pcd: true, avaliacao: false), Renda(100m), null, Hoje);

        resultado.Elegivel.Should().BeFalse();
        beneficio.Situacao.Should().Be(SituacaoBeneficio.Indeferida);
    }

    [Fact] // I-2 + B-4: idade 64 (abaixo de 65) e sem PCD e indeferido.
    public void Invariante_2_idade_abaixo_de_65_sem_pcd_e_indeferido()
    {
        var beneficio = Solicitar(TipoBeneficio.Bpc);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Bpc, SmJunho2026);

        var resultado = beneficio.AvaliarElegibilidade(criterio, Dados(idade: 64), Renda(100m), null, Hoje);

        resultado.Elegivel.Should().BeFalse();
    }

    [Fact] // I-4 + B-2: eventual elegivel quando renda exatamente = 1/2 SM (regra <= 1/2).
    public void Invariante_4_eventual_elegivel_no_meio_salario()
    {
        var beneficio = Solicitar(TipoBeneficio.Eventual);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Eventual, SmJunho2026);

        // 1412/2 = 706.
        var resultado = beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(706m), null, Hoje);

        resultado.Elegivel.Should().BeTrue();
        beneficio.Situacao.Should().Be(SituacaoBeneficio.Concedida);
    }

    [Fact] // I-4 + Cenario 5: eventual indeferido por renda > 1/2 SM.
    public void Invariante_4_eventual_indeferido_acima_de_meio_salario()
    {
        var beneficio = Solicitar(TipoBeneficio.Eventual);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Eventual, SmJunho2026);

        var resultado = beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(707m), null, Hoje);

        resultado.Elegivel.Should().BeFalse();
        beneficio.Situacao.Should().Be(SituacaoBeneficio.Indeferida);
    }

    [Fact] // I-5 + Cenario 8: beneficio ja decidido nao admite nova decisao.
    public void Invariante_5_beneficio_decidido_nao_admite_nova_decisao()
    {
        var beneficio = Solicitar(TipoBeneficio.Eventual);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Eventual, SmJunho2026);
        beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(100m), null, Hoje);
        beneficio.Situacao.Should().Be(SituacaoBeneficio.Concedida);

        ((Action)(() => beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(100m), null, Hoje)))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => beneficio.Conceder(null, Hoje))).Should().Throw<InvalidOperationException>();
        ((Action)(() => beneficio.Indeferir("motivo", Hoje))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-7 + B-5: indeferir com motivo vazio e rejeitado.
    public void Invariante_7_indeferir_sem_motivo_e_rejeitado()
    {
        var beneficio = Solicitar(TipoBeneficio.Eventual);

        ((Action)(() => beneficio.Indeferir("   ", Hoje))).Should().Throw<ArgumentException>();
        beneficio.Situacao.Should().Be(SituacaoBeneficio.EmAvaliacao);
    }

    [Fact] // I-8 + Cenario 6: entrega de cesta sobre beneficio nao concedido e rejeitada.
    public void Invariante_8_entrega_de_cesta_exige_concedido()
    {
        var beneficio = Solicitar(TipoBeneficio.Eventual);

        ((Action)(() => beneficio.EntregarCestaBasica(1, Hoje))).Should().Throw<InvalidOperationException>();
        beneficio.DomainEvents.OfType<CestaBasicaEntregue>().Should().BeEmpty();
    }

    [Fact] // I-8 + Cenario 7: entrega de cesta sobre beneficio nao-eventual (BPC) e rejeitada.
    public void Invariante_8_entrega_de_cesta_exige_tipo_eventual()
    {
        var beneficio = Solicitar(TipoBeneficio.Bpc);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Bpc, SmJunho2026);
        beneficio.AvaliarElegibilidade(criterio, Dados(idade: 70), Renda(100m), ValorMonetario.De(1412m), Hoje);
        beneficio.Situacao.Should().Be(SituacaoBeneficio.Concedida);

        ((Action)(() => beneficio.EntregarCestaBasica(1, Hoje))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8 + B-7: entrega de cesta com quantidade 0 e rejeitada.
    public void Invariante_8_entrega_de_cesta_quantidade_zero_e_rejeitada()
    {
        var beneficio = Solicitar(TipoBeneficio.Eventual);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Eventual, SmJunho2026);
        beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(100m), null, Hoje);

        ((Action)(() => beneficio.EntregarCestaBasica(0, Hoje))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-9 + B-6: conceder cesta basica com valor nulo e permitido (provisao em especie).
    public void Invariante_9_concessao_de_cesta_com_valor_nulo_e_permitida()
    {
        var beneficio = Solicitar(TipoBeneficio.Eventual);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Eventual, SmJunho2026);

        beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(100m), valorConcedido: null, Hoje);

        beneficio.Situacao.Should().Be(SituacaoBeneficio.Concedida);
        beneficio.Valor.Should().BeNull();
    }

    [Fact] // I-11 + B-8: solicitar para familia vazia e rejeitado.
    public void Invariante_11_solicitar_sem_familia_e_rejeitado()
    {
        ((Action)(() => Beneficio.Solicitar(TenantA, Guid.Empty, TipoBeneficio.Bpc, Junho2026)))
            .Should().Throw<ArgumentException>();
    }

    [Fact] // I-1: competencia invalida e rejeitada na solicitacao.
    public void Invariante_1_competencia_invalida_e_rejeitada()
    {
        ((Action)(() => Beneficio.Solicitar(TenantA, Familia1, TipoBeneficio.Bpc, default)))
            .Should().Throw<ArgumentException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // (nenhum) --Solicitar--> EmAvaliacao.
    public void Transicao_solicitar_nasce_em_avaliacao()
    {
        var beneficio = Solicitar(TipoBeneficio.Pbf);

        beneficio.Situacao.Should().Be(SituacaoBeneficio.EmAvaliacao);
        beneficio.EstaDecidido.Should().BeFalse();
    }

    [Fact] // EmAvaliacao --Conceder--> Concedida (via AvaliarElegibilidade).
    public void Transicao_conceder_de_avaliacao_para_concedida()
    {
        var beneficio = Solicitar(TipoBeneficio.Pbf);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Pbf, SmJunho2026);

        beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(100m), ValorMonetario.De(600m), Hoje);

        beneficio.Situacao.Should().Be(SituacaoBeneficio.Concedida);
        beneficio.Valor!.Valor.Should().Be(600m);
        beneficio.DomainEvents.OfType<BeneficioConcedido>().Should().ContainSingle();
    }

    [Fact] // EmAvaliacao --Indeferir--> Indeferida (via AvaliarElegibilidade).
    public void Transicao_indeferir_de_avaliacao_para_indeferida()
    {
        var beneficio = Solicitar(TipoBeneficio.Pbf);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Pbf, SmJunho2026);

        beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(5000m), null, Hoje);

        beneficio.Situacao.Should().Be(SituacaoBeneficio.Indeferida);
        beneficio.DomainEvents.OfType<BeneficioIndeferido>().Should().ContainSingle();
    }

    [Fact] // Cenario 4: Concedida --EntregarCestaBasica--> Concedida (mantem), emite CestaBasicaEntregue.
    public void Transicao_entregar_cesta_mantem_concedida()
    {
        var beneficio = Solicitar(TipoBeneficio.Eventual);
        var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Eventual, SmJunho2026);
        beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(100m), null, Hoje);

        beneficio.EntregarCestaBasica(3, Hoje);

        beneficio.Situacao.Should().Be(SituacaoBeneficio.Concedida);
        beneficio.QuantidadeCesta.Should().Be(3);
        beneficio.DataEntregaCesta.Should().Be(Hoje);
        beneficio.DomainEvents.OfType<CestaBasicaEntregue>().Should().ContainSingle();
    }

    [Fact] // Cenario 10: a regra aplicada e a vigente na competencia (SM diferentes).
    public void Cenario_10_regra_aplicada_e_a_vigente_na_competencia()
    {
        // Mesma renda (700), SMs distintos: com SM 1412 (1/2 = 706) eventual elegivel; com SM 1100 (1/2 = 550) indeferido.
        var elegivel = Solicitar(TipoBeneficio.Eventual);
        elegivel.AvaliarElegibilidade(
            CriterioElegibilidade.Vigente(TipoBeneficio.Eventual, ValorMonetario.De(1412m)), Dados(), Renda(700m), null, Hoje);

        var indeferido = Solicitar(TipoBeneficio.Eventual);
        indeferido.AvaliarElegibilidade(
            CriterioElegibilidade.Vigente(TipoBeneficio.Eventual, ValorMonetario.De(1100m)), Dados(), Renda(700m), null, Hoje);

        elegivel.Situacao.Should().Be(SituacaoBeneficio.Concedida);
        indeferido.Situacao.Should().Be(SituacaoBeneficio.Indeferida);
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 4: concessao + entrega persistem com auditoria.
    public async Task Cenario_4_concessao_e_entrega_persistem_com_auditoria()
    {
        BeneficioId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var beneficio = Solicitar(TipoBeneficio.Eventual);
            var criterio = CriterioElegibilidade.Vigente(TipoBeneficio.Eventual, SmJunho2026);
            beneficio.AvaliarElegibilidade(criterio, Dados(), Renda(100m), null, Hoje);
            id = beneficio.Id;
            contexto.Beneficios.Add(beneficio);
            await contexto.SaveChangesAsync();

            beneficio.EntregarCestaBasica(2, Hoje);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var beneficio = await contexto.Beneficios.SingleAsync(b => b.Id == id);
            beneficio.Situacao.Should().Be(SituacaoBeneficio.Concedida);
            beneficio.QuantidadeCesta.Should().Be(2);
            beneficio.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync()).Should().NotBeEmpty();
        }
    }

    [Fact] // Cenario 11: consulta de concessoes por competencia e tenant-scoped.
    public async Task Cenario_11_consulta_por_competencia_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            var beneficio = Beneficio.Solicitar(TenantA, Familia2, TipoBeneficio.Pbf, Junho2026);
            beneficio.Conceder(ValorMonetario.De(600m), Hoje);
            contexto.Beneficios.Add(beneficio);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Beneficios
                    .Where(b => b.Competencia == Junho2026 && b.Situacao == SituacaoBeneficio.Concedida)
                    .ToListAsync())
                .Should().BeEmpty();
        }
    }
}
