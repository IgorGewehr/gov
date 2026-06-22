using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="FolhaDePagamento"/>.</summary>
public interface IFolhaDePagamentoRepository
{
    /// <summary>Marca uma nova folha de pagamento para insercao.</summary>
    /// <param name="folha">Folha a adicionar.</param>
    void Adicionar(FolhaDePagamento folha);

    /// <summary>Obtem uma folha por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A folha, ou <c>null</c> se inexistente no tenant.</returns>
    Task<FolhaDePagamento?> ObterPorIdAsync(FolhaDePagamentoId id, CancellationToken cancellationToken);

    /// <summary>Obtem a folha de uma competencia no tenant atual (indice unico (TenantId, Competencia) — I-1).</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A folha da competencia, ou <c>null</c> se inexistente.</returns>
    Task<FolhaDePagamento?> ObterPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe folha para a competencia no tenant atual (I-1).</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir folha para a competencia.</returns>
    Task<bool> ExisteParaCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken);
}
