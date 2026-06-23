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

    /// <summary>Obtem a folha de uma competencia e tipo no tenant atual (indice unico (TenantId, Competencia, Tipo) — I-1).</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <param name="tipo">Tipo da folha (default <see cref="TipoFolha.Mensal"/>).</param>
    /// <returns>A folha da competencia/tipo, ou <c>null</c> se inexistente.</returns>
    Task<FolhaDePagamento?> ObterPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken, TipoFolha tipo = TipoFolha.Mensal);

    /// <summary>Indica se ja existe folha para a competencia e tipo no tenant atual (I-1).</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <param name="tipo">Tipo da folha (default <see cref="TipoFolha.Mensal"/>).</param>
    /// <returns><c>true</c> se ja existir folha para a competencia/tipo.</returns>
    Task<bool> ExisteParaCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken, TipoFolha tipo = TipoFolha.Mensal);

    /// <summary>
    /// Lista as folhas de um tipo cujas competencias caem num ano (tenant-scoped). Usado pelo
    /// autosservico para reunir, ex., as folhas de FERIAS de um servidor num exercicio — o handler
    /// filtra, em memoria, os eventos do PROPRIO servidor (dado-proprio).
    /// </summary>
    /// <param name="ano">Ano de referencia das competencias.</param>
    /// <param name="tipo">Tipo (natureza) da folha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Folhas do tipo/ano no tenant.</returns>
    Task<IReadOnlyList<FolhaDePagamento>> ListarPorTipoEAnoAsync(int ano, TipoFolha tipo, CancellationToken cancellationToken);

    /// <summary>
    /// Lista TODAS as folhas de uma competencia (qualquer tipo) no tenant atual — para a consolidacao
    /// fiscal mensal (P0-2): INSS/IRRF apurados sobre a soma das folhas da mesma competencia (Mensal +
    /// Ferias), respeitando o teto unico do INSS e a faixa correta do IRRF.
    /// </summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Todas as folhas da competencia no tenant.</returns>
    Task<IReadOnlyList<FolhaDePagamento>> ListarPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken);
}
