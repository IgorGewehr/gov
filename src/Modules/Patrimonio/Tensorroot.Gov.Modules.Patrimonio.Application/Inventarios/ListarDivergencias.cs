using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Lista as divergências apuradas de um inventário (tenant-scoped, read-only).</summary>
/// <param name="InventarioId">Inventário a consultar.</param>
public sealed record ListarDivergenciasQuery(Guid InventarioId) : IQuery<IReadOnlyList<DivergenciaInventarioDto>>;

/// <summary>Handler da listagem de divergências.</summary>
public sealed class ListarDivergenciasHandler(IInventarioRepository inventarios)
    : IQueryHandler<ListarDivergenciasQuery, IReadOnlyList<DivergenciaInventarioDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DivergenciaInventarioDto>> Handle(
        ListarDivergenciasQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inventario = await inventarios.ObterPorIdAsync(new InventarioId(request.InventarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventário não encontrado.");

        return inventario.Divergencias
            .Select(d => new DivergenciaInventarioDto(
                d.Id.Value,
                d.Tipo.ToString(),
                d.BemPatrimonialId?.Value,
                d.Descricao,
                d.Recomendacao.ToString()))
            .ToList();
    }
}
