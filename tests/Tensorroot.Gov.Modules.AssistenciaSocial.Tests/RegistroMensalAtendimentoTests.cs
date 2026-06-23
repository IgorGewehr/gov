using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Fiscal;
using Xunit;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Tests;

/// <summary>
/// A-2 — cobertura do Registro Mensal de Atendimentos (RMA): consolidacao DERIVADA do Prontuario SUAS
/// (sem dupla digitacao), reprodutibilidade (mesma fonte -> mesma consolidacao), fechamento/auditoria e
/// isolamento por tenant, sobre SQLite em memoria.
/// </summary>
public sealed class RegistroMensalAtendimentoTests : AssistenciaSocialTestBase
{
    private static readonly Guid CrasId = Guid.Parse("dddddddd-0000-0000-0000-0000000000c1");
    private static readonly Guid FamiliaA = Guid.Parse("dddddddd-0000-0000-0000-000000000a01");
    private static readonly Guid FamiliaB = Guid.Parse("dddddddd-0000-0000-0000-000000000a02");
    private static readonly Guid ProfId = Guid.Parse("dddddddd-0000-0000-0000-0000000000f1");
    private static readonly Competencia Junho2026 = Competencia.De(2026, 6);

    private static ProntuarioSuas ProntuarioComAtendimentos(Guid familiaId, params DateOnly[] datasPaif)
    {
        var prontuario = ProntuarioSuas.Abrir(TenantA, familiaId, CrasId, new DateOnly(2026, 6, 1));
        foreach (var data in datasPaif)
        {
            prontuario.RegistrarAtendimento(TipoServico.Paif, data, "atendimento PAIF", ProfId, TipoUnidadeAtendimento.Cras);
        }

        return prontuario;
    }

    [Fact]
    public void RMA_consolida_as_contagens_por_servico_e_e_fechavel()
    {
        var rma = RegistroMensalAtendimento.Abrir(TenantA, CrasId, TipoUnidadeAtendimento.Cras, Junho2026);

        var contagens = new Dictionary<TipoServico, int> { [TipoServico.Paif] = 5, [TipoServico.Scfv] = 2 };
        rma.ConsolidarDoProntuario(contagens);

        rma.QuantidadeDoServico(TipoServico.Paif).Should().Be(5);
        rma.QuantidadeDoServico(TipoServico.Scfv).Should().Be(2);
        rma.TotalAtendimentos.Should().Be(7);
        rma.Situacao.Should().Be(SituacaoRma.Aberto);

        rma.Fechar(new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc));
        rma.Situacao.Should().Be(SituacaoRma.Fechado);
        rma.DomainEvents.OfType<RmaFechado>().Should().ContainSingle()
            .Which.TotalAtendimentos.Should().Be(7);
    }

    [Fact]
    public void RMA_fechado_nao_admite_reconsolidacao_nem_novo_fechamento()
    {
        var rma = RegistroMensalAtendimento.Abrir(TenantA, CrasId, TipoUnidadeAtendimento.Cras, Junho2026);
        rma.ConsolidarDoProntuario(new Dictionary<TipoServico, int> { [TipoServico.Paif] = 1 });
        rma.Fechar(DateTime.UtcNow);

        ((Action)(() => rma.ConsolidarDoProntuario(new Dictionary<TipoServico, int> { [TipoServico.Paif] = 9 })))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => rma.Fechar(DateTime.UtcNow))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RMA_reconsolidar_substitui_as_linhas_anteriores_idempotente()
    {
        var rma = RegistroMensalAtendimento.Abrir(TenantA, CrasId, TipoUnidadeAtendimento.Cras, Junho2026);

        rma.ConsolidarDoProntuario(new Dictionary<TipoServico, int> { [TipoServico.Paif] = 3 });
        rma.ConsolidarDoProntuario(new Dictionary<TipoServico, int> { [TipoServico.Paif] = 3 }); // mesma fonte

        // Reprodutibilidade: reconsolidar com a mesma fonte nao duplica linhas nem soma.
        rma.QuantidadeDoServico(TipoServico.Paif).Should().Be(3);
        rma.Linhas.Should().ContainSingle();
    }

    [Fact]
    public async Task RMA_consolida_DO_PRONTUARIO_existente_sem_dupla_digitacao()
    {
        // Arrange: dois prontuarios da mesma unidade com atendimentos PAIF em junho/2026 (e um fora do mes).
        await using (var ctx = CriarContexto(TenantA))
        {
            ctx.Prontuarios.Add(ProntuarioComAtendimentos(FamiliaA, new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 20)));
            ctx.Prontuarios.Add(ProntuarioComAtendimentos(FamiliaB, new DateOnly(2026, 6, 10), new DateOnly(2026, 5, 30))); // maio fica de fora
            await ctx.SaveChangesAsync();
        }

        // Act: o read model conta direto do prontuario (fonte unica) e o RMA consolida.
        int totalPrimeiraPassada;
        await using (var ctx = CriarContexto(TenantA))
        {
            var readModel = new ConsolidacaoRmaReadModel(ctx);
            var contagens = await readModel.ContarAtendimentosPorServicoAsync(CrasId, Junho2026, CancellationToken.None);

            contagens.Should().ContainKey(TipoServico.Paif);
            contagens[TipoServico.Paif].Should().Be(3); // 2 + 1 em junho; o de maio nao entra

            var rma = RegistroMensalAtendimento.Abrir(TenantA, CrasId, TipoUnidadeAtendimento.Cras, Junho2026);
            rma.ConsolidarDoProntuario(contagens);
            ctx.RegistrosMensaisAtendimento.Add(rma);
            await ctx.SaveChangesAsync();
            totalPrimeiraPassada = rma.TotalAtendimentos;
        }

        totalPrimeiraPassada.Should().Be(3);

        // Reprodutibilidade: recontar a mesma fonte da o mesmo resultado.
        await using (var ctx = CriarContexto(TenantA))
        {
            var readModel = new ConsolidacaoRmaReadModel(ctx);
            var contagens = await readModel.ContarAtendimentosPorServicoAsync(CrasId, Junho2026, CancellationToken.None);
            contagens[TipoServico.Paif].Should().Be(3);
        }
    }

    [Fact]
    public async Task RMA_persiste_e_isola_por_tenant()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            var rma = RegistroMensalAtendimento.Abrir(TenantA, CrasId, TipoUnidadeAtendimento.Cras, Junho2026);
            rma.ConsolidarDoProntuario(new Dictionary<TipoServico, int> { [TipoServico.Paif] = 4 });
            ctx.RegistrosMensaisAtendimento.Add(rma);
            await ctx.SaveChangesAsync();

            (await ctx.AuditTrail.AnyAsync()).Should().BeTrue();
        }

        // O tenant B nao enxerga o RMA do tenant A (Global Query Filter), e o read model do B conta zero.
        await using (var ctx = CriarContexto(TenantB))
        {
            (await ctx.RegistrosMensaisAtendimento.CountAsync()).Should().Be(0);

            var readModel = new ConsolidacaoRmaReadModel(ctx);
            var contagens = await readModel.ContarAtendimentosPorServicoAsync(CrasId, Junho2026, CancellationToken.None);
            contagens.Should().BeEmpty();
        }
    }
}
