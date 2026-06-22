using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Lookups;

/// <summary>
/// Implementacao EF Core das consultas de leitura das Unidades de Atendimento (CRAS/CREAS) do
/// tenant — satisfaz <see cref="IUnidadeAtendimentoRepository"/> (cobertura territorial — I-2) e
/// <see cref="IUnidadeAtendimentoTipoLookup"/> (compatibilidade servico↔unidade — I-4). Sempre
/// tenant-scoped via Global Query Filter.
/// </summary>
public sealed class UnidadeAtendimentoLookupRepository(AssistenciaSocialDbContext context)
    : IUnidadeAtendimentoRepository, IUnidadeAtendimentoTipoLookup
{
    /// <inheritdoc />
    public async Task<UnidadeAtendimentoResumo?> ObterPorIdAsync(Guid unidadeAtendimentoId, CancellationToken cancellationToken)
    {
        var unidade = await context.UnidadesAtendimento
            .FirstOrDefaultAsync(item => item.Id == unidadeAtendimentoId, cancellationToken)
            .ConfigureAwait(false);

        return unidade is null
            ? null
            : new UnidadeAtendimentoResumo(unidade.Id, unidade.TerritorioCobertura);
    }

    /// <inheritdoc />
    public async Task<TipoUnidadeAtendimento?> ObterTipoAsync(Guid unidadeAtendimentoId, CancellationToken cancellationToken)
    {
        var unidade = await context.UnidadesAtendimento
            .FirstOrDefaultAsync(item => item.Id == unidadeAtendimentoId, cancellationToken)
            .ConfigureAwait(false);

        return unidade?.Tipo;
    }
}
