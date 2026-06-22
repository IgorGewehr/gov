using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Fornece os <see cref="ParametrosFolha"/> vigentes para o tenant atual (teto remuneratorio,
/// rubrica de abate-teto, prazos). A implementacao (na Infrastructure) resolve a configuracao por
/// tenant — os valores nunca sao <em>hardcoded</em> no dominio/Application (CLAUDE.md S7).
/// </summary>
public interface IParametrosFolhaProvider
{
    /// <summary>Obtem os parametros de folha vigentes para o tenant atual.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Parametros de folha do tenant.</returns>
    Task<ParametrosFolha> ObterAsync(CancellationToken cancellationToken);
}
