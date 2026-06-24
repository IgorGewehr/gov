using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Pasep;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="ApuracaoPasep"/>.</summary>
public interface IApuracaoPasepRepository
{
    /// <summary>Marca uma nova apuracao para insercao.</summary>
    /// <param name="apuracao">Apuracao a adicionar.</param>
    void Adicionar(ApuracaoPasep apuracao);

    /// <summary>Obtem a apuracao de uma competencia (respeitando o filtro de tenant).</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A apuracao, ou <c>null</c> se inexistente no tenant.</returns>
    Task<ApuracaoPasep?> ObterPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken);

    /// <summary>Verifica se ja existe apuracao para a competencia no tenant (unicidade).</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existe.</returns>
    Task<bool> ExisteParaCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken);

    /// <summary>Lista as apuracoes de um ano (ordenadas por mes); para a navegabilidade/painel.</summary>
    /// <param name="ano">Ano das competencias.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Apuracoes do ano.</returns>
    Task<IReadOnlyList<ApuracaoPasep>> ListarPorAnoAsync(int ano, CancellationToken cancellationToken);
}
