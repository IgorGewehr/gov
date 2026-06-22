using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="RubricaFolha"/> (catalogo parametrizavel de verbas).</summary>
public interface IRubricaFolhaRepository
{
    /// <summary>Marca uma nova rubrica para insercao.</summary>
    /// <param name="rubrica">Rubrica a adicionar.</param>
    void Adicionar(RubricaFolha rubrica);

    /// <summary>Obtem uma rubrica por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A rubrica, ou <c>null</c> se inexistente no tenant.</returns>
    Task<RubricaFolha?> ObterPorIdAsync(RubricaFolhaId id, CancellationToken cancellationToken);

    /// <summary>Obtem a rubrica ativa pelo codigo vigente na competencia, no tenant atual.</summary>
    /// <param name="codigo">Codigo da rubrica (S-1010).</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A rubrica vigente, ou <c>null</c> se inexistente.</returns>
    Task<RubricaFolha?> ObterVigentePorCodigoAsync(Rubrica codigo, Competencia competencia, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe rubrica ativa com o codigo no tenant atual (unicidade).</summary>
    /// <param name="codigo">Codigo da rubrica.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir rubrica ativa com o codigo.</returns>
    Task<bool> CodigoExisteAsync(Rubrica codigo, CancellationToken cancellationToken);

    /// <summary>Lista as rubricas ativas vigentes na competencia, no tenant atual.</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Rubricas vigentes ordenadas por codigo.</returns>
    Task<IReadOnlyList<RubricaFolha>> ListarVigentesAsync(Competencia competencia, CancellationToken cancellationToken);
}
