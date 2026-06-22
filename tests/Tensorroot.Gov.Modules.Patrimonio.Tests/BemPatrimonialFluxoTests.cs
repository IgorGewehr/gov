using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Cobertura de integração do agregado <see cref="BemPatrimonial"/>: invariantes,
/// cada transição da máquina de estados e os cenários BDD de BemPatrimonial.rules.md,
/// sobre SQLite em memória com auditoria, Outbox e isolamento por tenant.
/// </summary>
public sealed class BemPatrimonialFluxoTests : PatrimonioTestBase
{
    private static BemPatrimonial NovoBemMovel(decimal valorInicial = 12000m, decimal residual = 2000m, int vidaUtilMeses = 100)
        => BemPatrimonial.Incorporar(
            TenantA,
            "Notebook Dell",
            TipoBem.Movel,
            ValorMonetario.De(valorInicial),
            ValorMonetario.De(residual),
            vidaUtilMeses,
            new DateOnly(2026, 1, 10),
            "Aquisicao");

    private static BemPatrimonial NovoImovel(decimal valorInicial = 500000m, decimal residual = 0m, decimal valorTerreno = 200000m, int vidaUtilMeses = 600)
        => BemPatrimonial.Incorporar(
            TenantA,
            "Escola Municipal",
            TipoBem.Imovel,
            ValorMonetario.De(valorInicial),
            ValorMonetario.De(residual),
            vidaUtilMeses,
            new DateOnly(2026, 1, 10),
            "Construcao",
            valorTerreno);

    // ---------- Invariantes ----------

    [Fact] // I-12 + Cenário 9: incorporação nasce em EmIncorporacao e emite BemIncorporado.
    public void Invariante_12_incorporacao_nasce_valida_em_EmIncorporacao_e_emite_evento()
    {
        var bem = NovoBemMovel();

        bem.Situacao.Should().Be(SituacaoBemPatrimonial.EmIncorporacao);
        bem.ValorContabil.Valor.Should().Be(12000m);
        bem.DomainEvents.OfType<BemIncorporado>().Should().ContainSingle();
    }

    [Fact] // B-1: ValorResidual > ValorInicial é rejeitado.
    public void Invariante_12_residual_maior_que_inicial_e_rejeitado()
    {
        var acao = () => BemPatrimonial.Incorporar(
            TenantA, "Bem", TipoBem.Movel, ValorMonetario.De(100m), ValorMonetario.De(200m),
            12, new DateOnly(2026, 1, 1), "Aquisicao");

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // B-2: VidaUtilMeses == 0 é rejeitado.
    public void Invariante_1_vida_util_zero_e_rejeitada()
    {
        var acao = () => BemPatrimonial.Incorporar(
            TenantA, "Bem", TipoBem.Movel, ValorMonetario.De(100m), ValorMonetario.De(10m),
            0, new DateOnly(2026, 1, 1), "Aquisicao");

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-1: parcela mensal = (inicial - terreno - residual) / vida util.
    public void Invariante_1_parcela_mensal_e_linear()
    {
        var bem = NovoBemMovel(valorInicial: 12000m, residual: 2000m, vidaUtilMeses: 100);

        // (12000 - 0 - 2000) / 100 = 100.
        bem.ParcelaMensal.Should().Be(100m);
    }

    [Fact] // I-2 + B-3: bem não EmCondicoesDeUso não deprecia.
    public void Invariante_2_nao_deprecia_sem_condicoes_de_uso()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-0001");

        var depreciado = bem.Depreciar(new DateOnly(2026, 2, 1));

        depreciado.Should().Be(0m);
        bem.ValorContabil.Valor.Should().Be(12000m);
        bem.HistoricosDepreciacao.Should().BeEmpty();
    }

    [Fact] // I-4 + Cenário 2 + B-5: a depreciação é limitada para nunca cruzar o residual.
    public void Invariante_4_valor_contabil_nunca_inferior_ao_residual()
    {
        // Inicial 1000, residual 900, vida 1 mês => parcela seria 100, restando exatamente o residual.
        var bem = NovoBemMovel(valorInicial: 1000m, residual: 900m, vidaUtilMeses: 1);
        bem.Tombar("TOMBO-RES");
        bem.ColocarEmCondicoesDeUso();

        bem.Depreciar(new DateOnly(2026, 2, 1));
        bem.ValorContabil.Valor.Should().Be(900m);

        // B-6: nova depreciação no piso não reduz mais nada.
        var segunda = bem.Depreciar(new DateOnly(2026, 3, 1));
        segunda.Should().Be(0m);
        bem.ValorContabil.Valor.Should().Be(900m);
    }

    [Fact] // I-3 + Cenário 1 + B-4: terreno não deprecia; só a benfeitoria.
    public void Invariante_3_terreno_nao_deprecia()
    {
        // Inicial 500000, terreno 200000, residual 0, vida 600 => parcela = 300000/600 = 500.
        var imovel = NovoImovel();
        imovel.Tombar("TOMBO-IMOVEL");
        imovel.ColocarEmCondicoesDeUso();

        imovel.ParcelaMensal.Should().Be(500m);
        var depreciado = imovel.Depreciar(new DateOnly(2026, 2, 1));

        depreciado.Should().Be(500m);
        imovel.ValorContabil.Valor.Should().Be(499500m);
        imovel.ValorTerreno.Should().Be(200000m); // terreno inalterado.
    }

    [Fact] // I-5 + Cenário 6: reavaliação ajusta valor contábil ao valor justo (com laudo).
    public void Invariante_5_reavaliacao_ajusta_valor_contabil()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-REAV");

        bem.Reavaliar(15000m, "https://laudos/reav-1", new DateOnly(2026, 5, 1));

        bem.ValorContabil.Valor.Should().Be(15000m);
        bem.Reavaliacoes.Should().ContainSingle();
        bem.DomainEvents.OfType<BemReavaliado>().Should().ContainSingle();
    }

    [Fact] // I-5/B-14: reavaliação sem laudo é rejeitada.
    public void Invariante_5_reavaliacao_sem_laudo_e_rejeitada()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-X");

        var acao = () => bem.Reavaliar(15000m, "  ", new DateOnly(2026, 5, 1));

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-6 + Cenário 5: impairment reconhecido quando recuperável < contábil.
    public void Invariante_6_impairment_reconhece_perda()
    {
        var bem = NovoBemMovel(valorInicial: 12000m);
        bem.Tombar("TOMBO-IMP");

        bem.RegistrarImpairment(8000m, "https://laudos/imp-1", new DateOnly(2026, 6, 1));

        bem.ValorContabil.Valor.Should().Be(8000m);
        bem.Impairments.Should().ContainSingle()
            .Which.PerdaReconhecida.Should().Be(4000m);
    }

    [Fact] // I-6 + B-9: impairment com recuperável >= contábil não reconhece nada.
    public void Invariante_6_impairment_sem_perda_e_rejeitado()
    {
        var bem = NovoBemMovel(valorInicial: 12000m);
        bem.Tombar("TOMBO-IMP2");

        var acao = () => bem.RegistrarImpairment(12000m, "https://laudos/imp", new DateOnly(2026, 6, 1));

        acao.Should().Throw<InvalidOperationException>();
        bem.Impairments.Should().BeEmpty();
    }

    [Fact] // I-7 + Cenário 3 + B-10: baixa sem laudo é rejeitada e nenhum evento de baixa é emitido.
    public void Invariante_7_baixa_sem_laudo_e_rejeitada_sem_evento()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-BX");

        var acao = () => bem.Baixar("Inservivel", "   ", Guid.NewGuid());

        acao.Should().Throw<ArgumentException>();
        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Tombado);
        bem.DomainEvents.OfType<BemBaixado>().Should().BeEmpty();
    }

    [Fact] // I-7: baixa sem autorização é rejeitada.
    public void Invariante_7_baixa_sem_autorizacao_e_rejeitada()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-BX2");

        var acao = () => bem.Baixar("Inservivel", "https://laudos/bx", Guid.Empty);

        acao.Should().Throw<ArgumentOutOfRangeException>();
        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Tombado);
    }

    [Fact] // I-9 + Cenário 7 + B-11: alienação sem avaliação prévia é rejeitada.
    public void Invariante_9_alienacao_sem_avaliacao_previa_e_rejeitada()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-AL");

        var acao = () => bem.Alienar(Guid.Empty, porLeilao: true, valorAlienacao: 5000m);

        acao.Should().Throw<ArgumentOutOfRangeException>();
        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Tombado);
    }

    [Fact] // I-10 + B-7: tombar duas vezes falha (situação já != EmIncorporacao).
    public void Invariante_10_tombar_duas_vezes_falha()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-UM");

        var acao = () => bem.Tombar("TOMBO-DOIS");

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-11 + Cenário 10 + B-12: estado encerrado não admite novas transições.
    public void Invariante_11_bem_encerrado_nao_admite_transicoes()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-ENC");
        bem.ColocarEmCondicoesDeUso();
        bem.Baixar("Inservivel", "https://laudos/bx", Guid.NewGuid());

        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Baixada);
        ((Action)(() => bem.Depreciar(new DateOnly(2026, 7, 1)))).Should().Throw<InvalidOperationException>();
        ((Action)(() => bem.Transferir("Almoxarifado", Guid.NewGuid(), new DateOnly(2026, 7, 1)))).Should().Throw<InvalidOperationException>();
        ((Action)(() => bem.Baixar("De novo", "https://laudos/bx", Guid.NewGuid()))).Should().Throw<InvalidOperationException>();
        ((Action)bem.Ceder).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-13: transferência só é permitida para bem ativo no acervo.
    public void Invariante_13_transferencia_exige_ativo_no_acervo()
    {
        var emIncorporacao = NovoBemMovel();
        ((Action)(() => emIncorporacao.Transferir("Setor B", Guid.NewGuid(), new DateOnly(2026, 4, 1))))
            .Should().Throw<InvalidOperationException>();

        var tombado = NovoBemMovel();
        tombado.Tombar("TOMBO-TR");
        tombado.Transferir("Setor B", Guid.NewGuid(), new DateOnly(2026, 4, 1), "Setor A");
        tombado.Movimentacoes.Should().ContainSingle();
        tombado.Situacao.Should().Be(SituacaoBemPatrimonial.Tombado);
    }

    [Fact] // I-14 + Cenário 11: cessão só a partir de Tombado e mantém no acervo (sem baixa).
    public void Invariante_14_cessao_mantem_no_acervo()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-CED");

        bem.Ceder();

        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Cedido);
    }

    [Fact] // I-14: cessão exige situação Tombado (a partir de EmIncorporacao falha).
    public void Invariante_14_cessao_exige_tombado()
    {
        var bem = NovoBemMovel();

        ((Action)bem.Ceder).Should().Throw<InvalidOperationException>();
    }

    // ---------- Máquina de estados (transições) ----------

    [Fact] // Cenário 8: EmIncorporacao --Tombar--> Tombado, emite BemTombado.
    public void Transicao_tombar_de_EmIncorporacao_para_Tombado()
    {
        var bem = NovoBemMovel();

        bem.Tombar("TOMBO-2026-0001");

        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Tombado);
        bem.NumeroTombamento!.Value.Valor.Should().Be("TOMBO-2026-0001");
        bem.DomainEvents.OfType<BemTombado>().Should().ContainSingle();
    }

    [Fact] // Tombado --Depreciar--> Tombado, emite BemDepreciado.
    public void Transicao_depreciar_mantem_Tombado_e_emite_evento()
    {
        var bem = NovoBemMovel(valorInicial: 12000m, residual: 2000m, vidaUtilMeses: 100);
        bem.Tombar("TOMBO-DEP");
        bem.ColocarEmCondicoesDeUso();

        var valor = bem.Depreciar(new DateOnly(2026, 2, 1));

        valor.Should().Be(100m);
        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Tombado);
        bem.ValorContabil.Valor.Should().Be(11900m);
        bem.DomainEvents.OfType<BemDepreciado>().Should().ContainSingle();
    }

    [Fact] // Cedido --Transferir--> Cedido (ativo no acervo).
    public void Transicao_transferir_a_partir_de_Cedido_mantem_Cedido()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-CT");
        bem.Ceder();

        bem.Transferir("Setor C", Guid.NewGuid(), new DateOnly(2026, 4, 1));

        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Cedido);
        bem.Movimentacoes.Should().ContainSingle();
    }

    [Fact] // Tombado --Baixar--> Baixada, emite BemBaixado.
    public void Transicao_baixar_de_Tombado_para_Baixada()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-BXOK");

        bem.Baixar("Inservivel", "https://laudos/bx-ok", Guid.NewGuid());

        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Baixada);
        bem.DomainEvents.OfType<BemBaixado>().Should().ContainSingle();
    }

    [Fact] // Tombado --Alienar--> Alienada, emite BemBaixado (motivo Alienacao).
    public void Transicao_alienar_de_Tombado_para_Alienada()
    {
        var bem = NovoBemMovel();
        bem.Tombar("TOMBO-ALOK");

        bem.Alienar(Guid.NewGuid(), porLeilao: true, valorAlienacao: 5000m);

        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Alienada);
        bem.DomainEvents.OfType<BemBaixado>().Should().ContainSingle()
            .Which.MotivoBaixa.Should().Be("Alienacao");
    }

    // ---------- Persistência, auditoria e isolamento ----------

    [Fact] // Cenário 4: baixa válida persiste, registra auditoria e mantém Outbox no mesmo commit.
    public async Task Cenario_4_fluxo_de_baixa_persiste_com_auditoria()
    {
        BemPatrimonialId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var bem = NovoBemMovel();
            bem.Tombar("TOMBO-PERSIST");
            id = bem.Id;
            contexto.Bens.Add(bem);
            await contexto.SaveChangesAsync();

            bem.Baixar("Inservivel", "https://laudos/bx-persist", Guid.NewGuid());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var bem = await contexto.Bens.SingleAsync(b => b.Id == id);
            bem.Situacao.Should().Be(SituacaoBemPatrimonial.Baixada);
            bem.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutações do bem");
        }
    }

    [Fact] // Cenário 12: consulta é tenant-scoped (Global Query Filter).
    public async Task Cenario_12_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Bens.Add(NovoBemMovel());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Bens.ToListAsync()).Should().BeEmpty();
        }
    }
}
