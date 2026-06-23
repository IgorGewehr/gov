using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Relatorios;

/// <summary>
/// Demonstrativo de despesa de pessoal para o Tribunal de Contas numa competencia (tenant-scoped,
/// read-only — ONDA3-DESIGN §4.1). Visao gerencial; a remessa formatada SIAPC/PAD e do modulo
/// Transparencia/M4.
/// </summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia.</param>
public sealed record ObterDemonstrativoTceQuery(int Ano, int Mes) : IQuery<DemonstrativoTceView?>;

/// <summary>Handler do demonstrativo de pessoal para o TCE.</summary>
public sealed class ObterDemonstrativoTceHandler(IRelatoriosFolhaConsulta consulta)
    : IQueryHandler<ObterDemonstrativoTceQuery, DemonstrativoTceView?>
{
    /// <inheritdoc />
    public async Task<DemonstrativoTceView?> Handle(ObterDemonstrativoTceQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await consulta
            .ObterDemonstrativoTceAsync(request.Ano, request.Mes, cancellationToken)
            .ConfigureAwait(false);
    }
}
