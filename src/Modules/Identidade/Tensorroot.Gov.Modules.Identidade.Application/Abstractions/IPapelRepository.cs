using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;

namespace Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Papel"/> (sempre tenant-scoped).</summary>
public interface IPapelRepository
{
    /// <summary>Marca um novo papel para insercao.</summary>
    /// <param name="papel">Papel a adicionar.</param>
    void Adicionar(Papel papel);

    /// <summary>Obtem um papel por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O papel, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Papel?> ObterPorIdAsync(PapelId id, CancellationToken cancellationToken);

    /// <summary>Obtem os papeis correspondentes ao conjunto de identificadores informado (tenant-scoped).</summary>
    /// <param name="ids">Identificadores a buscar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Papeis encontrados no tenant.</returns>
    Task<IReadOnlyList<Papel>> ObterPorIdsAsync(IReadOnlyCollection<PapelId> ids, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe um papel com o nome informado no tenant.</summary>
    /// <param name="nome">Nome do papel.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o nome ja estiver em uso no tenant.</returns>
    Task<bool> NomeEmUsoAsync(string nome, CancellationToken cancellationToken);

    /// <summary>Lista os papeis do tenant.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Papeis do tenant.</returns>
    Task<IReadOnlyList<Papel>> ListarAsync(CancellationToken cancellationToken);
}
