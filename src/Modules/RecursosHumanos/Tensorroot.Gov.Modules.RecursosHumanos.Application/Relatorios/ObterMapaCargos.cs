using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Relatorios;

/// <summary>
/// Mapa de cargos (ocupados x vagos) do quadro de pessoal (tenant-scoped, read-only — ONDA3-DESIGN §4.1).
/// </summary>
public sealed record ObterMapaCargosQuery : IQuery<MapaCargosView>;

/// <summary>Handler do mapa de cargos.</summary>
public sealed class ObterMapaCargosHandler(IRelatoriosFolhaConsulta consulta)
    : IQueryHandler<ObterMapaCargosQuery, MapaCargosView>
{
    /// <inheritdoc />
    public async Task<MapaCargosView> Handle(ObterMapaCargosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await consulta.ObterMapaCargosAsync(cancellationToken).ConfigureAwait(false);
    }
}
