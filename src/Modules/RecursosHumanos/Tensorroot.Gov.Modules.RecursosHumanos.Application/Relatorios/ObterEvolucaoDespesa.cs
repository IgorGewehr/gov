using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Relatorios;

/// <summary>
/// Evolucao mensal da despesa de pessoal entre duas competencias (tenant-scoped, read-only —
/// ONDA3-DESIGN §4.1). Serie mensal — base para o acompanhamento do limite da LRF (art. 19/20).
/// </summary>
/// <param name="AnoDe">Ano da competencia inicial.</param>
/// <param name="MesDe">Mes da competencia inicial.</param>
/// <param name="AnoAte">Ano da competencia final.</param>
/// <param name="MesAte">Mes da competencia final.</param>
public sealed record ObterEvolucaoDespesaQuery(int AnoDe, int MesDe, int AnoAte, int MesAte)
    : IQuery<EvolucaoDespesaPessoalView>;

/// <summary>Handler da evolucao mensal da despesa de pessoal.</summary>
public sealed class ObterEvolucaoDespesaHandler(IRelatoriosFolhaConsulta consulta)
    : IQueryHandler<ObterEvolucaoDespesaQuery, EvolucaoDespesaPessoalView>
{
    /// <inheritdoc />
    public async Task<EvolucaoDespesaPessoalView> Handle(ObterEvolucaoDespesaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await consulta
            .ObterEvolucaoDespesaAsync(request.AnoDe, request.MesDe, request.AnoAte, request.MesAte, cancellationToken)
            .ConfigureAwait(false);
    }
}
