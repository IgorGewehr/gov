using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

namespace Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="UnidadeOrganizacional"/> (sempre tenant-scoped).</summary>
public interface IUnidadeRepository
{
    /// <summary>Marca uma nova UO para insercao.</summary>
    /// <param name="unidade">UO a adicionar.</param>
    void Adicionar(UnidadeOrganizacional unidade);

    /// <summary>Obtem uma UO por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A UO, ou <c>null</c> se inexistente no tenant.</returns>
    Task<UnidadeOrganizacional?> ObterPorIdAsync(UnidadeOrganizacionalId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe uma UO com o codigo informado no tenant.</summary>
    /// <param name="codigo">Codigo estavel (sera normalizado pela mesma regra do dominio).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o codigo ja estiver em uso no tenant.</returns>
    Task<bool> CodigoEmUsoAsync(string codigo, CancellationToken cancellationToken);

    /// <summary>
    /// Lista TODAS as UOs do tenant (ativas e inativas) — base para montar a arvore
    /// (<see cref="ArvoreUnidades"/>), resolver ancestrais/descendentes e projetar a estrutura.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>UOs do tenant.</returns>
    Task<IReadOnlyList<UnidadeOrganizacional>> ListarAsync(CancellationToken cancellationToken);
}
