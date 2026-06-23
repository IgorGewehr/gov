using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Inventario"/>.</summary>
public interface IInventarioRepository
{
    /// <summary>Marca um novo inventário para inserção.</summary>
    /// <param name="inventario">Inventário a adicionar.</param>
    void Adicionar(Inventario inventario);

    /// <summary>Obtém um inventário por identificador (respeitando o filtro de tenant), com itens e divergências.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O inventário, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Inventario?> ObterPorIdAsync(InventarioId id, CancellationToken cancellationToken);

    /// <summary>
    /// Materializa o snapshot contábil do acervo para a abertura do inventário, filtrado opcionalmente
    /// por setor (localização). Lê os bens ativos no acervo (Tombado/Cedido) do tenant.
    /// </summary>
    /// <param name="setor">Setor/localização escopo; nulo = todo o acervo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Snapshots dos bens a inventariar.</returns>
    Task<IReadOnlyList<SnapshotBem>> CarregarSnapshotAcervoAsync(string? setor, CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de inventários por exercício/setor/situação. Tenant-scoped via Global Query Filter.
    /// Ordena por exercício decrescente e data de abertura.
    /// </summary>
    /// <param name="exercicio">Filtro opcional por exercício.</param>
    /// <param name="setor">Filtro opcional por setor (case-insensível).</param>
    /// <param name="situacao">Filtro opcional por situação.</param>
    /// <param name="pagina">Página (base 1).</param>
    /// <param name="tamanho">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Página de inventários e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Inventario> Itens, int Total)> BuscarAsync(
        int? exercicio,
        string? setor,
        SituacaoInventario? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
