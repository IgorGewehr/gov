using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Consulta o regime previdenciario vigente de um servidor (I-4: efetivo -> RPPS / S-1202;
/// temporario/comissionado/celetista -> RGPS / S-1200). Abstrai o agregado Servidor (mesmo modulo)
/// para o lancamento de eventos de folha, sem acoplar o handler ao seu repositorio concreto.
/// </summary>
public interface IServidorRegimeConsulta
{
    /// <summary>Obtem o regime previdenciario do servidor no tenant atual.</summary>
    /// <param name="servidorId">Identificador do servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O regime previdenciario, ou <c>null</c> se o servidor nao existir no tenant.</returns>
    Task<RegimePrevidenciario?> ObterRegimeAsync(Guid servidorId, CancellationToken cancellationToken);
}
