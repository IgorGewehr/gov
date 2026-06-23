using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;

/// <summary>
/// <b>A-0 — fonte do criterio MUNICIPAL de beneficio eventual</b> (modalidade habilitada + corte de
/// renda em multiplos de SM, ou sem corte), versionado por tenant+vigencia. Os criterios sao 100% de
/// LEI/DECRETO MUNICIPAL + CMAS (LOAS art. 22, Lei 12.435/2011; Decreto 6.307/2007) — NAO ha default
/// federal de "1/4 SM" (revogado). A ausencia de criterio para a modalidade/competencia significa que a
/// lei municipal nao a habilitou: NAO se inventa um teto federal (B-11/§16). A implementacao reside na
/// Infraestrutura (tabela parametrizavel por tenant/vigencia).
/// </summary>
public interface ICriterioBeneficioEventualProvider
{
    /// <summary>
    /// Obtem o criterio municipal vigente para a modalidade na competencia, ou <c>null</c> quando a lei
    /// municipal nao a habilitou para a competencia (decisao nunca usa default federal).
    /// </summary>
    /// <param name="modalidade">Modalidade do beneficio eventual.</param>
    /// <param name="competencia">Competencia (ano/mes) de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O criterio municipal vigente, ou <c>null</c> se nao parametrizado.</returns>
    Task<CriterioBeneficioEventual?> ObterCriterioVigenteAsync(
        ModalidadeBeneficioEventual modalidade,
        Competencia competencia,
        CancellationToken cancellationToken);
}
