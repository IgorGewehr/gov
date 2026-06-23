using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Relatorios;

/// <summary>
/// Folha por secretaria/UO e por fonte (regime) numa competencia (tenant-scoped, read-only —
/// ONDA3-DESIGN §4.1). Projeta sobre a folha mensal/servidores/cargos existentes.
/// </summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
public sealed record ObterFolhaPorSecretariaQuery(int Ano, int Mes) : IQuery<FolhaPorSecretariaView?>;

/// <summary>Handler da folha por secretaria/UO e fonte.</summary>
public sealed class ObterFolhaPorSecretariaHandler(IRelatoriosFolhaConsulta consulta)
    : IQueryHandler<ObterFolhaPorSecretariaQuery, FolhaPorSecretariaView?>
{
    /// <inheritdoc />
    public async Task<FolhaPorSecretariaView?> Handle(ObterFolhaPorSecretariaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await consulta
            .ObterFolhaPorSecretariaAsync(request.Ano, request.Mes, cancellationToken)
            .ConfigureAwait(false);
    }
}
