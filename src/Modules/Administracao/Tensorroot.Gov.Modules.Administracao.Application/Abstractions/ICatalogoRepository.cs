using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="ItemCatalogo"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface ICatalogoRepository
{
    /// <summary>Marca um novo item para insercao.</summary>
    /// <param name="item">Item a adicionar.</param>
    void Adicionar(ItemCatalogo item);

    /// <summary>Obtem um item por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O item, ou <c>null</c> se inexistente no tenant.</returns>
    Task<ItemCatalogo?> ObterPorIdAsync(ItemCatalogoId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe item com o codigo informado no tenant (unicidade).</summary>
    /// <param name="codigo">Codigo padronizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja houver item com esse codigo no tenant.</returns>
    Task<bool> ExistePorCodigoAsync(string codigo, CancellationToken cancellationToken);

    /// <summary>Lista itens do tenant, com filtro opcional por natureza e termo de busca (codigo/descricao).</summary>
    /// <param name="natureza">Natureza a filtrar (opcional).</param>
    /// <param name="termo">Termo de busca em codigo/descricao (opcional).</param>
    /// <param name="apenasAtivos">Quando verdadeiro, retorna apenas itens Ativos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Itens do tenant.</returns>
    Task<IReadOnlyList<ItemCatalogo>> ListarAsync(
        NaturezaItem? natureza,
        string? termo,
        bool apenasAtivos,
        CancellationToken cancellationToken);
}
