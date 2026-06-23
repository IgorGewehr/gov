using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="ItemEstoque"/>.</summary>
public interface IItemEstoqueRepository
{
    /// <summary>Marca um novo item de estoque para inserção.</summary>
    /// <param name="item">Item a adicionar.</param>
    void Adicionar(ItemEstoque item);

    /// <summary>Obtém um item de estoque por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O item, ou <c>null</c> se inexistente no tenant.</returns>
    Task<ItemEstoque?> ObterPorIdAsync(ItemEstoqueId id, CancellationToken cancellationToken);

    /// <summary>Indica se já existe um item com o código informado no tenant atual (I-10).</summary>
    /// <param name="codigo">Código do item.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o código já existir.</returns>
    Task<bool> CodigoExisteAsync(string codigo, CancellationToken cancellationToken);

    /// <summary>Lista os itens ativos cujo saldo está abaixo ou igual ao ponto de pedido (gatilho de reposição).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Itens a repor.</returns>
    Task<IReadOnlyList<ItemEstoque>> ListarAbaixoDoPontoPedidoAsync(CancellationToken cancellationToken);

    /// <summary>Lista todos os itens ativos do tenant (base para agregações, ex.: Curva ABC).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Itens ativos do tenant.</returns>
    Task<IReadOnlyList<ItemEstoque>> ListarAtivosAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de itens de almoxarifado por código/descrição (navegabilidade — Onda 0), com
    /// filtros opcionais por situação e classe ABC. Tenant-scoped via Global Query Filter. Ordena por código.
    /// </summary>
    /// <param name="termo">Termo livre (código ou descrição; case/acento-insensível); nulo lista tudo.</param>
    /// <param name="situacao">Filtro opcional por situação (ativo/inativo).</param>
    /// <param name="classificacaoAbc">Filtro opcional por classe na Curva ABC.</param>
    /// <param name="pagina">Página (base 1).</param>
    /// <param name="tamanho">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Página de itens e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<ItemEstoque> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoItemEstoque? situacao,
        CurvaABC? classificacaoAbc,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
