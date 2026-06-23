using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>
/// <b>M7.0.3.</b> Fornece os percentuais mínimos vigentes por setor (parametrizáveis por
/// tenant+vigência — CLAUDE.md §7/§16). Default legal: Saúde 15% (LC 141/2012), Educação 25%
/// (CF art. 212). Nunca hardcoded em regra: o provider lê de <c>ParametroVigente</c> e só usa o
/// default legal quando o tenant não tem parâmetro próprio para a vigência.
/// </summary>
public interface IParametroMinimoProvider
{
    /// <summary>Obtém os percentuais mínimos vigentes dos setores no exercício.</summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Parâmetros mínimos por setor.</returns>
    Task<IReadOnlyList<ParametroMinimo>> ObterParametrosAsync(int exercicio, CancellationToken cancellationToken);
}
