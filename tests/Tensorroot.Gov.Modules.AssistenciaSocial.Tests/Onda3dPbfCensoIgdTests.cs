using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Censo;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Censo;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Igd;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Censo;
using Xunit;
using TipoServicoSuas = Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios.TipoServico;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Tests;

/// <summary>
/// Onda 3d — cobertura-chave de PBF (condicionalidade descumprida -> efeito gradativo + evento de busca
/// ativa), Censo SUAS (consolida o volume DERIVADO do RMA da unidade) e EstimativaIgd (estima a partir de
/// indicadores locais). Reusa Familia/RMA/Beneficio por Id; isolado por tenant.
/// </summary>
public sealed class Onda3dPbfCensoIgdTests : AssistenciaSocialTestBase
{
    private static readonly Competencia Junho2026 = Competencia.De(2026, 6);
    private static readonly FamiliaId FamiliaBeneficiaria = new(Guid.Parse("eeeeeeee-0000-0000-0000-000000000a01"));
    private static readonly Guid Membro1 = Guid.Parse("eeeeeeee-0000-0000-0000-0000000000b1");
    private static readonly Guid Membro2 = Guid.Parse("eeeeeeee-0000-0000-0000-0000000000b2");
    private static readonly Guid CrasId = Guid.Parse("eeeeeeee-0000-0000-0000-0000000000c1");
    private static readonly Guid ProfId = Guid.Parse("eeeeeeee-0000-0000-0000-0000000000f1");

    // ---------- 3d.1 — PBF: condicionalidade descumprida -> efeito ----------

    [Fact] // I-4/I-6: 2 descumprimentos efetivos escalam para bloqueio e disparam a busca ativa do CRAS.
    public void Pbf_descumprimento_de_condicionalidade_aplica_efeito_gradativo_e_emite_evento()
    {
        var acompanhamento = AcompanhamentoCondicionalidade.Abrir(TenantA, FamiliaBeneficiaria, Junho2026);

        // Primeiro descumprimento: advertencia (sem evento de impacto).
        acompanhamento.RegistrarCondicionalidade(TipoCondicionalidade.EducacaoFrequenciaEscolar, Membro1, StatusCondicionalidade.Descumprida, "faltas no bimestre");
        acompanhamento.Efeito.Should().Be(EfeitoDescumprimento.Advertencia);
        acompanhamento.DomainEvents.OfType<CondicionalidadeDescumprida>().Should().BeEmpty();

        // Segundo descumprimento efetivo: escala para bloqueio -> evento de busca ativa (>= bloqueio).
        acompanhamento.RegistrarCondicionalidade(TipoCondicionalidade.SaudeVacinacaoNutricaoInfantil, Membro2, StatusCondicionalidade.Descumprida, "vacina em atraso");

        acompanhamento.DescumprimentosEfetivos.Should().Be(2);
        acompanhamento.Efeito.Should().Be(EfeitoDescumprimento.Bloqueio);
        acompanhamento.DomainEvents.OfType<CondicionalidadeDescumprida>().Should().ContainSingle()
            .Which.Efeito.Should().Be(EfeitoDescumprimento.Bloqueio);
    }

    [Fact] // I-5: justificar um descumprimento reduz a contagem efetiva e recalcula o efeito.
    public void Pbf_justificar_descumprimento_reduz_o_efeito_gradativo()
    {
        var acompanhamento = AcompanhamentoCondicionalidade.Abrir(TenantA, FamiliaBeneficiaria, Junho2026);
        var registroId = acompanhamento.RegistrarCondicionalidade(TipoCondicionalidade.EducacaoFrequenciaEscolar, Membro1, StatusCondicionalidade.Descumprida, "faltas");
        acompanhamento.RegistrarCondicionalidade(TipoCondicionalidade.SaudePreNatalGestante, Membro2, StatusCondicionalidade.Descumprida, "sem pre-natal");
        acompanhamento.Efeito.Should().Be(EfeitoDescumprimento.Bloqueio);

        acompanhamento.JustificarDescumprimento(registroId, "internacao hospitalar comprovada");

        acompanhamento.DescumprimentosEfetivos.Should().Be(1);
        acompanhamento.Efeito.Should().Be(EfeitoDescumprimento.Advertencia);
    }

    // ---------- 3d.2 — Censo SUAS: consolida o volume DERIVADO do RMA ----------

    [Fact] // I-9: o Censo consolida o volume anual DERIVANDO do somatorio dos RMAs da unidade no exercicio.
    public async Task Censo_consolida_o_volume_anual_derivado_do_RMA_da_unidade()
    {
        // Arrange: dois RMAs (junho e julho/2026) da mesma unidade, e um de 2025 (que nao entra no exercicio).
        await using (var ctx = CriarContexto(TenantA))
        {
            ctx.RegistrosMensaisAtendimento.Add(RmaCom(Competencia.De(2026, 6), TipoServicoSuas.Paif, 5));
            ctx.RegistrosMensaisAtendimento.Add(RmaCom(Competencia.De(2026, 7), TipoServicoSuas.Scfv, 3));
            ctx.RegistrosMensaisAtendimento.Add(RmaCom(Competencia.De(2025, 6), TipoServicoSuas.Paif, 99)); // outro exercicio
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var consolidacao = new ConsolidacaoCensoReadModel(ctx);
            var volume = await consolidacao.SomarAtendimentosDoExercicioAsync(CrasId, 2026, CancellationToken.None);

            // 5 (junho) + 3 (julho) = 8; o RMA de 2025 nao entra.
            volume.Should().Be(8);
        }
    }

    [Fact] // I-9/I-10: o formulario do Censo consolida (idempotente) os indicadores da unidade no exercicio.
    public void Censo_consolidar_e_idempotente_e_fechamento_sela_o_exercicio()
    {
        var unidade = UnidadeSocioassistencial.Cadastrar(TenantA, "CRAS Centro", TipoUnidadeAtendimento.Cras, "Centro", "Rua A, 1");
        unidade.OfertarServico(TipoServicoSuas.Paif, 50);
        unidade.DefinirEquipe(6);

        var formulario = FormularioCensoSuas.Abrir(TenantA, unidade.Id, 2026);
        formulario.Consolidar(unidade.QuantidadeProfissionais, unidade.Servicos.Count, familiasReferenciadas: 120, volumeAtendimentosAno: 8);
        formulario.Consolidar(unidade.QuantidadeProfissionais, unidade.Servicos.Count, familiasReferenciadas: 120, volumeAtendimentosAno: 8); // idempotente

        formulario.VolumeAtendimentosAno.Should().Be(8);
        formulario.QuantidadeProfissionais.Should().Be(6);
        formulario.FamiliasReferenciadas.Should().Be(120);

        formulario.Fechar(new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc));
        formulario.Situacao.Should().Be(SituacaoCenso.Fechado);
        ((Action)(() => formulario.Consolidar(6, 1, 120, 8))).Should().Throw<InvalidOperationException>();
    }

    // ---------- 3d.3 — EstimativaIgd: estima a partir de indicadores locais ----------

    [Fact] // I-12/I-13: o indice e a media dos tres fatores locais [0,1] e carrega o rotulo de estimativa local.
    public void Igd_estimativa_calcula_indice_plausivel_a_partir_de_fatores_locais()
    {
        var fatores = new FatoresIgd(
            FatorAtualizacaoCadastral: 0.90m,
            FatorCondicionalidades: 0.75m,
            FatorGestaoBeneficios: 0.60m);

        var estimativa = CalculadoraIgd.Estimar(fatores);

        // Media de (0.90 + 0.75 + 0.60) / 3 = 0.75 (numero plausivel, em [0,1]).
        estimativa.Indice.Should().Be(0.75m);
        estimativa.Indice.Should().BeInRange(0m, 1m);
        estimativa.Rotulo.Should().Be(CalculadoraIgd.RotuloEstimativaLocal);
    }

    [Fact] // I-12: fatores fora de [0,1] sao clampeados (robustez) antes da media.
    public void Igd_estimativa_clampeia_fatores_fora_do_intervalo()
    {
        var estimativa = CalculadoraIgd.Estimar(new FatoresIgd(1.5m, 1m, 1m));

        estimativa.Fatores.FatorAtualizacaoCadastral.Should().Be(1m);
        estimativa.Indice.Should().Be(1m);
    }

    private static RegistroMensalAtendimento RmaCom(Competencia competencia, TipoServicoSuas servico, int quantidade)
    {
        var rma = RegistroMensalAtendimento.Abrir(TenantA, CrasId, TipoUnidadeAtendimento.Cras, competencia);
        rma.ConsolidarDoProntuario(new Dictionary<TipoServicoSuas, int> { [servico] = quantidade });
        return rma;
    }
}
