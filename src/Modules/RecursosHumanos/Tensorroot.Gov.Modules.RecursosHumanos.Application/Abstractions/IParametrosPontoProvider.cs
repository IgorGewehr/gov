using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Fornece os <see cref="ParametrosPonto"/> vigentes do tenant (config), nunca hardcoded.</summary>
public interface IParametrosPontoProvider
{
    /// <summary>Obtem os parametros de ponto do tenant corrente.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Parametros de ponto vigentes.</returns>
    Task<ParametrosPonto> ObterAsync(CancellationToken cancellationToken);
}
