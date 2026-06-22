using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Familia"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IFamiliaRepository
{
    /// <summary>Marca uma nova familia para insercao.</summary>
    /// <param name="familia">Familia a adicionar.</param>
    void Adicionar(Familia familia);

    /// <summary>Obtem uma familia por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A familia, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Familia?> ObterPorIdAsync(FamiliaId id, CancellationToken cancellationToken);

    /// <summary>Lista as familias de um territorio do tenant.</summary>
    /// <param name="territorio">Territorio de cobertura.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Familias do territorio no tenant atual.</returns>
    Task<IReadOnlyList<Familia>> ListarPorTerritorioAsync(string territorio, CancellationToken cancellationToken);
}
