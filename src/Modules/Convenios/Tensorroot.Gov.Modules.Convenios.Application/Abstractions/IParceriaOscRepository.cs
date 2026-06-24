using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;

namespace Tensorroot.Gov.Modules.Convenios.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="ParceriaOsc"/> (fluxo B). Tenant-scoped pelo Global Query Filter do
/// DbContext; as escritas sao confirmadas pelo <c>IUnitOfWork</c> do pipeline.
/// </summary>
public interface IParceriaOscRepository
{
    /// <summary>Adiciona uma nova parceria ao contexto.</summary>
    /// <param name="parceria">Parceria a adicionar.</param>
    void Adicionar(ParceriaOsc parceria);

    /// <summary>Carrega uma parceria pelo identificador (com filhos), ou nulo se nao existir no tenant.</summary>
    /// <param name="id">Identificador da parceria.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A parceria, ou nulo.</returns>
    Task<ParceriaOsc?> ObterPorIdAsync(ParceriaOscId id, CancellationToken cancellationToken);

    /// <summary>Lista as parcerias do tenant (read-side), opcionalmente filtrando por situacao.</summary>
    /// <param name="situacao">Situacao (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Parcerias do tenant.</returns>
    Task<IReadOnlyList<ParceriaOsc>> ListarAsync(SituacaoParceriaOsc? situacao, CancellationToken cancellationToken);
}
