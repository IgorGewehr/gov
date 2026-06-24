using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="ContaFinanceira"/> (tesouraria caixa-banco).</summary>
public interface IContaFinanceiraRepository
{
    /// <summary>Marca uma nova conta para inserção.</summary>
    /// <param name="conta">Conta a adicionar.</param>
    void Adicionar(ContaFinanceira conta);

    /// <summary>Obtém uma conta por identificador (com movimentos, respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conta, ou <c>null</c>.</returns>
    Task<ContaFinanceira?> ObterPorIdAsync(ContaFinanceiraId id, CancellationToken cancellationToken);

    /// <summary>Lista todas as contas do tenant (sem movimentos).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de contas.</returns>
    Task<IReadOnlyList<ContaFinanceira>> ListarAsync(CancellationToken cancellationToken);
}
