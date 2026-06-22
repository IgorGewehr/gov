using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>
/// Deriva a data-limite legal/parametrizada de transmissao de uma declaracao fiscal, por tenant
/// (nunca hardcoded): MSC ate o ultimo dia do mes subsequente a competencia; RREO/RGF ate 30 dias
/// apos o bimestre/quadrimestre; DCA conforme calendario anual STN (I-11; LRF art. 23 paragrafo 3).
/// </summary>
public interface ICalendarioFiscal
{
    /// <summary>Calcula a data-limite de transmissao para o tipo/periodo informados.</summary>
    /// <param name="tipo">Tipo da declaracao.</param>
    /// <param name="exercicio">Ano de exercicio.</param>
    /// <param name="mes">Mes da competencia (MSC), quando aplicavel.</param>
    /// <param name="numeroBimestre">Numero do bimestre (RREO), quando aplicavel.</param>
    /// <param name="numeroQuadrimestre">Numero do quadrimestre (RGF), quando aplicavel.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Data-limite de transmissao.</returns>
    Task<DateOnly> DerivarDataLimiteAsync(
        TipoDeclaracaoFiscal tipo,
        int exercicio,
        int? mes,
        int? numeroBimestre,
        int? numeroQuadrimestre,
        CancellationToken cancellationToken);
}
