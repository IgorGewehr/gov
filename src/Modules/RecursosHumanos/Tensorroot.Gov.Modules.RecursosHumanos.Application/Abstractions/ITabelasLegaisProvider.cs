using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Fornece as tabelas legais (INSS/IRRF/RPPS) vigentes na competencia para o tenant atual. A
/// implementacao (Infrastructure) seleciona a versao cuja vigencia inicial e a mais recente menor ou
/// igual a competencia. Os valores sao dado parametrizado por exercicio, jamais hardcoded (S16).
/// </summary>
public interface ITabelasLegaisProvider
{
    /// <summary>Obtem a tabela INSS (RGPS) vigente na competencia, ou <c>null</c> se inexistente.</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tabela INSS vigente ou <c>null</c>.</returns>
    Task<TabelaInss?> ObterInssVigenteAsync(Competencia competencia, CancellationToken cancellationToken);

    /// <summary>Obtem a tabela IRRF vigente na competencia, ou <c>null</c> se inexistente.</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tabela IRRF vigente ou <c>null</c>.</returns>
    Task<TabelaIrrf?> ObterIrrfVigenteAsync(Competencia competencia, CancellationToken cancellationToken);

    /// <summary>Obtem a tabela RPPS municipal vigente na competencia, ou <c>null</c> se inexistente (fail-closed no motor).</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tabela RPPS vigente ou <c>null</c>.</returns>
    Task<TabelaRpps?> ObterRppsVigenteAsync(Competencia competencia, CancellationToken cancellationToken);
}
