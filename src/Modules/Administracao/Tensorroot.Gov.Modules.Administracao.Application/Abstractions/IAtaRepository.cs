using Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Ata"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IAtaRepository
{
    /// <summary>Marca uma nova ata para insercao.</summary>
    /// <param name="ata">Ata a adicionar.</param>
    void Adicionar(Ata ata);

    /// <summary>Obtem uma ata por identificador (com itens e adesoes carregados).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ata, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Ata?> ObterPorIdAsync(AtaId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe ata com o numero informado no tenant (unicidade).</summary>
    /// <param name="numero">Numero da ata.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja houver ata com esse numero no tenant.</returns>
    Task<bool> ExistePorNumeroAsync(string numero, CancellationToken cancellationToken);

    /// <summary>Lista as atas do tenant na situacao informada (ou todas quando nula).</summary>
    /// <param name="situacao">Situacao a filtrar (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Atas do tenant.</returns>
    Task<IReadOnlyList<Ata>> ListarAsync(SituacaoAta? situacao, CancellationToken cancellationToken);
}
