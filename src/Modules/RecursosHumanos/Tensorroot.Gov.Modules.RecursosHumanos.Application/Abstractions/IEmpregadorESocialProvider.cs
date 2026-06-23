using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Fornece os <see cref="ParametrosESocial"/> vigentes do tenant (empregador/EFR/ambiente) a partir da
/// configuracao (Key Vault/IOptions), pois NAO ha agregado de "empregador" no RH (ESOCIAL-SPEC §1.1).
/// </summary>
public interface IEmpregadorESocialProvider
{
    /// <summary>Obtem os parametros eSocial do tenant atual.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Parametros eSocial (empregador/EFR/ambiente).</returns>
    Task<ParametrosESocial> ObterAsync(CancellationToken cancellationToken);
}
