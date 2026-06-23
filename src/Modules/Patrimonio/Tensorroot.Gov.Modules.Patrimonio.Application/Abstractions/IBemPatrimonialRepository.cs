using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="BemPatrimonial"/>.</summary>
public interface IBemPatrimonialRepository
{
    /// <summary>Marca um novo bem patrimonial para inserção.</summary>
    /// <param name="bemPatrimonial">Bem a adicionar.</param>
    void Adicionar(BemPatrimonial bemPatrimonial);

    /// <summary>Obtém um bem por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O bem, ou <c>null</c> se inexistente no tenant.</returns>
    Task<BemPatrimonial?> ObterPorIdAsync(BemPatrimonialId id, CancellationToken cancellationToken);

    /// <summary>Lista os bens depreciáveis do tenant (Tombado, em condições de uso, valor contábil acima do residual).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Bens depreciáveis do tenant.</returns>
    Task<IReadOnlyList<BemPatrimonial>> ListarDepreciaveisAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de bens por descrição/número de tombamento (navegabilidade — Onda 0), com filtros
    /// opcionais por tipo e situação. Tenant-scoped via Global Query Filter. Ordena por descrição.
    /// </summary>
    /// <param name="termo">Termo livre (descrição ou número de tombamento; case/acento-insensível); nulo lista tudo.</param>
    /// <param name="tipo">Filtro opcional por tipo (móvel/imóvel).</param>
    /// <param name="situacao">Filtro opcional por situação no ciclo patrimonial.</param>
    /// <param name="pagina">Página (base 1).</param>
    /// <param name="tamanho">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Página de bens e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<BemPatrimonial> Itens, int Total)> BuscarAsync(
        string? termo,
        TipoBem? tipo,
        SituacaoBemPatrimonial? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
