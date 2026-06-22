using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Verifica se uma rubrica existe e esta vigente na tabela eSocial S-1010 na competencia (I-3 / §11).
/// A implementacao (Infrastructure) consulta a tabela de rubricas do tenant.
/// </summary>
public interface IRubricaS1010Consulta
{
    /// <summary>Indica se a rubrica esta vigente em S-1010 na competencia informada.</summary>
    /// <param name="rubrica">Rubrica a verificar.</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a rubrica existir e estiver vigente.</returns>
    Task<bool> EstaVigenteAsync(Rubrica rubrica, Competencia competencia, CancellationToken cancellationToken);
}
