using Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;

namespace Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;

/// <summary>
/// Porta de persistência do read model consolidado por exercício (<see cref="IndicadorMunicipioSnapshot"/>).
/// Tenant-scoped pelo Global Query Filter do DbContext do módulo.
/// </summary>
public interface IIndicadorMunicipioRepository
{
    /// <summary>Obtém o consolidado do exercício (ou nulo se ainda não materializado).</summary>
    /// <param name="exercicio">Exercício (ano).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O snapshot do exercício ou nulo.</returns>
    Task<IndicadorMunicipioSnapshot?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo snapshot de exercício.</summary>
    /// <param name="snapshot">Snapshot a adicionar.</param>
    void Adicionar(IndicadorMunicipioSnapshot snapshot);
}
