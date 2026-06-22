using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Fornecedor"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IFornecedorRepository
{
    /// <summary>Marca um novo fornecedor para insercao.</summary>
    /// <param name="fornecedor">Fornecedor a adicionar.</param>
    void Adicionar(Fornecedor fornecedor);

    /// <summary>Obtem um fornecedor por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O fornecedor, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Fornecedor?> ObterPorIdAsync(FornecedorId id, CancellationToken cancellationToken);

    /// <summary>Obtem um fornecedor pelo CNPJ no tenant atual.</summary>
    /// <param name="cnpj">CNPJ normalizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O fornecedor, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Fornecedor?> ObterPorCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe um fornecedor com o CNPJ informado no tenant atual (unicidade I-4).</summary>
    /// <param name="cnpj">CNPJ normalizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja houver um fornecedor com esse CNPJ no tenant.</returns>
    Task<bool> ExistePorCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken);

    /// <summary>Lista os fornecedores com sancao impeditiva vigente na data de referencia (tenant-scoped).</summary>
    /// <param name="referencia">Data de referencia para a vigencia impeditiva.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Fornecedores impedidos do tenant.</returns>
    Task<IReadOnlyList<Fornecedor>> ListarImpedidosAsync(DateOnly referencia, CancellationToken cancellationToken);
}
