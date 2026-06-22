using Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="OrdemDePagamento"/>.</summary>
public interface IOrdemDePagamentoRepository
{
    /// <summary>Marca uma nova ordem de pagamento para inserção.</summary>
    /// <param name="ordem">Ordem a adicionar.</param>
    void Adicionar(OrdemDePagamento ordem);

    /// <summary>Obtém uma ordem por identificador (com itens, respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ordem, ou <c>null</c>.</returns>
    Task<OrdemDePagamento?> ObterPorIdAsync(OrdemDePagamentoId id, CancellationToken cancellationToken);
}
