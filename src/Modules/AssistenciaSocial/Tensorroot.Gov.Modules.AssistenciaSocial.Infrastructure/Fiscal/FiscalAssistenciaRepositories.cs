using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Fiscal;

/// <summary>Implementacao EF Core do repositorio do Fundo Municipal de Assistencia Social (A-1).</summary>
public sealed class FundoMunicipalAssistenciaRepository(AssistenciaSocialDbContext context) : IFundoMunicipalAssistenciaRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(FundoMunicipalAssistencia fundo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fundo);
        await context.FundosMunicipaisAssistencia.AddAsync(fundo, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FundoMunicipalAssistencia?> ObterPorIdAsync(FundoMunicipalAssistenciaId id, CancellationToken cancellationToken)
        => await context.FundosMunicipaisAssistencia
            .FirstOrDefaultAsync(fundo => fundo.Id == id, cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Implementacao EF Core do repositorio do Registro Mensal de Atendimentos (A-2).</summary>
public sealed class RegistroMensalAtendimentoRepository(AssistenciaSocialDbContext context) : IRegistroMensalAtendimentoRepository
{
    /// <inheritdoc />
    public async Task AdicionarAsync(RegistroMensalAtendimento rma, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rma);
        await context.RegistrosMensaisAtendimento.AddAsync(rma, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RegistroMensalAtendimento?> ObterPorIdAsync(RegistroMensalAtendimentoId id, CancellationToken cancellationToken)
        => await context.RegistrosMensaisAtendimento
            .FirstOrDefaultAsync(rma => rma.Id == id, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<RegistroMensalAtendimento?> ObterPorUnidadeCompetenciaAsync(Guid unidadeAtendimentoId, Competencia competencia, CancellationToken cancellationToken)
        => await context.RegistrosMensaisAtendimento
            .FirstOrDefaultAsync(rma => rma.UnidadeAtendimentoId == unidadeAtendimentoId && rma.Competencia == competencia, cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>
/// <b>A-2 — implementacao EF Core de <see cref="IConsolidacaoRmaReadModel"/>.</b> DERIVA as contagens
/// por servico diretamente do Prontuario SUAS/atendimentos (mesma base, tenant-scoped), contando os
/// <c>RegistroAcompanhamento</c> cujo <c>DataAtendimento</c> cai no mes da competencia, agrupados por
/// servico, nos prontuarios da unidade. Evita dupla digitacao (a fonte e o acompanhamento, nao planilha).
/// </summary>
public sealed class ConsolidacaoRmaReadModel(AssistenciaSocialDbContext context) : IConsolidacaoRmaReadModel
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<TipoServico, int>> ContarAtendimentosPorServicoAsync(
        Guid unidadeAtendimentoId,
        Competencia competencia,
        CancellationToken cancellationToken)
    {
        var inicio = new DateOnly(competencia.Ano, competencia.Mes, 1);
        var fim = inicio.AddMonths(1); // exclusivo

        // Consolida do prontuario: registros (entidades-filhas) das familias acompanhadas na unidade,
        // no mes da competencia. O Global Query Filter por tenant garante o isolamento (I-9).
        var contagens = await context.Prontuarios
            .Where(prontuario => prontuario.UnidadeAtendimentoId == unidadeAtendimentoId)
            .SelectMany(prontuario => prontuario.Registros)
            .Where(registro => registro.DataAtendimento >= inicio && registro.DataAtendimento < fim)
            .GroupBy(registro => registro.Servico)
            .Select(grupo => new { Servico = grupo.Key, Quantidade = grupo.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return contagens.ToDictionary(item => item.Servico, item => item.Quantidade);
    }
}
