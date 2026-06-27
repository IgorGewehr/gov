using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core do repositorio do agregado <see cref="ProcessoTrabalhista"/>.</summary>
public sealed class ProcessoTrabalhistaRepository(RecursosHumanosDbContext context) : IProcessoTrabalhistaRepository
{
    /// <inheritdoc />
    public void Adicionar(ProcessoTrabalhista processo)
    {
        ArgumentNullException.ThrowIfNull(processo);
        context.ProcessosTrabalhistas.Add(processo);
    }

    /// <inheritdoc />
    public Task<ProcessoTrabalhista?> ObterPorIdAsync(ProcessoTrabalhistaId id, CancellationToken cancellationToken)
        => context.ProcessosTrabalhistas.FirstOrDefaultAsync(processo => processo.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExisteNumeroAsync(string numeroProcesso, CancellationToken cancellationToken)
        => context.ProcessosTrabalhistas.AnyAsync(processo => processo.NumeroProcesso == numeroProcesso, cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ProcessoTrabalhista> Itens, int Total)> BuscarAsync(
        SituacaoProcessoTrabalhista? situacao,
        PrognosticoPerda? prognostico,
        string? termo,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        var consulta = context.ProcessosTrabalhistas.AsQueryable();

        if (situacao is { } filtroSituacao)
        {
            consulta = consulta.Where(processo => processo.Situacao == filtroSituacao);
        }

        if (prognostico is { } filtroPrognostico)
        {
            consulta = consulta.Where(processo => processo.Prognostico == filtroPrognostico);
        }

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var alvo = termo.Trim();
            consulta = consulta.Where(processo =>
                EF.Functions.Like(processo.NumeroProcesso, $"%{alvo}%") ||
                EF.Functions.Like(processo.Reclamante, $"%{alvo}%"));
        }

        var total = await consulta.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await consulta
            .OrderByDescending(processo => processo.DataAjuizamento)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    /// <inheritdoc />
    public async Task<decimal> SomarProvisaoVigenteAsync(CancellationToken cancellationToken)
        => await context.ProcessosTrabalhistas
            .Where(processo => processo.Situacao == SituacaoProcessoTrabalhista.EmAndamento
                && processo.Prognostico == PrognosticoPerda.Provavel)
            .SumAsync(processo => processo.ValorProvisionado, cancellationToken)
            .ConfigureAwait(false);
}
