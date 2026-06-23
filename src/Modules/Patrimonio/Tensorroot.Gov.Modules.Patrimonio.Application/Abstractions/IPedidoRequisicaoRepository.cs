using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="PedidoRequisicao"/>.</summary>
public interface IPedidoRequisicaoRepository
{
    /// <summary>Marca um novo pedido de requisição para inserção.</summary>
    /// <param name="pedido">Pedido a adicionar.</param>
    void Adicionar(PedidoRequisicao pedido);

    /// <summary>Obtém um pedido por identificador (respeitando o filtro de tenant), com suas linhas.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O pedido, ou <c>null</c> se inexistente no tenant.</returns>
    Task<PedidoRequisicao?> ObterPorIdAsync(PedidoRequisicaoId id, CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de pedidos por situação e setor/UO. Tenant-scoped via Global Query Filter.
    /// Ordena por data decrescente (fila de pendentes primeiro).
    /// </summary>
    /// <param name="situacao">Filtro opcional por situação (fila de aprovação/atendimento).</param>
    /// <param name="setor">Filtro opcional por setor (case-insensível).</param>
    /// <param name="unidadeId">Filtro opcional por UO consumidora.</param>
    /// <param name="pagina">Página (base 1).</param>
    /// <param name="tamanho">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Página de pedidos e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<PedidoRequisicao> Itens, int Total)> BuscarAsync(
        SituacaoPedido? situacao,
        string? setor,
        Guid? unidadeId,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
