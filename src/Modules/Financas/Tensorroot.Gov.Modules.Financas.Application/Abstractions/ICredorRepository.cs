using Tensorroot.Gov.Modules.Financas.Domain.Credores;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="CredorCadastrado"/> (cadastro de credores/fornecedores).</summary>
public interface ICredorRepository
{
    /// <summary>Marca um novo credor para inserção.</summary>
    /// <param name="credor">Credor a adicionar.</param>
    void Adicionar(CredorCadastrado credor);

    /// <summary>Obtém um credor por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O credor, ou <c>null</c>.</returns>
    Task<CredorCadastrado?> ObterPorIdAsync(CredorId id, CancellationToken cancellationToken);

    /// <summary>Indica se já existe credor com o documento informado no tenant (unicidade).</summary>
    /// <param name="documento">Documento normalizado (sem máscara).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já existe.</returns>
    Task<bool> ExisteComDocumentoAsync(string documento, CancellationToken cancellationToken);

    /// <summary>Obtém um credor pelo documento normalizado (CPF/CNPJ sem máscara).</summary>
    /// <param name="documento">Documento normalizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O credor, ou <c>null</c>.</returns>
    Task<CredorCadastrado?> ObterPorDocumentoAsync(string documento, CancellationToken cancellationToken);

    /// <summary>Lista credores do tenant, opcionalmente filtrando por termo (nome/documento).</summary>
    /// <param name="termo">Termo de busca (nome ou documento); <c>null</c> = todos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de credores.</returns>
    Task<IReadOnlyList<CredorCadastrado>> ListarAsync(string? termo, CancellationToken cancellationToken);
}
