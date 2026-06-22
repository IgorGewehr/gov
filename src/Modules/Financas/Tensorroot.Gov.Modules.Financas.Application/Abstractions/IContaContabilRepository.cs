using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="ContaContabil"/> (plano de contas).</summary>
public interface IContaContabilRepository
{
    /// <summary>Marca uma nova conta para inserção.</summary>
    /// <param name="conta">Conta a adicionar.</param>
    void Adicionar(ContaContabil conta);

    /// <summary>Obtém uma conta pelo código PCASP canônico.</summary>
    /// <param name="codigo">Código (ex.: <c>6.2.2.1.1.00.00</c>).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conta, ou <c>null</c>.</returns>
    Task<ContaContabil?> ObterPorCodigoAsync(string codigo, CancellationToken cancellationToken);

    /// <summary>Obtém uma conta por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conta, ou <c>null</c>.</returns>
    Task<ContaContabil?> ObterPorIdAsync(ContaContabilId id, CancellationToken cancellationToken);

    /// <summary>Lista todas as contas do tenant (para montar a árvore/seed).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contas do plano.</returns>
    Task<IReadOnlyList<ContaContabil>> ListarTodasAsync(CancellationToken cancellationToken);

    /// <summary>Indica se já existe conta com o código informado (idempotência do seed).</summary>
    /// <param name="codigo">Código.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já existir.</returns>
    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken);
}
