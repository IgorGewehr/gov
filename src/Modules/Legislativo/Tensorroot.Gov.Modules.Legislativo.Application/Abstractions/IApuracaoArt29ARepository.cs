using Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="ApuracaoArt29A"/> (demonstrativo do art. 29-A por exercicio).</summary>
public interface IApuracaoArt29ARepository
{
    /// <summary>Marca uma nova apuracao para insercao.</summary>
    /// <param name="apuracao">Apuracao a adicionar.</param>
    void Adicionar(ApuracaoArt29A apuracao);

    /// <summary>Obtem a apuracao por identificador (com as despesas), respeitando o tenant.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A apuracao, ou <c>null</c> se inexistente no tenant.</returns>
    Task<ApuracaoArt29A?> ObterPorIdAsync(ApuracaoArt29AId id, CancellationToken cancellationToken);

    /// <summary>Obtem a apuracao de um exercicio (a chave logica e tenant + exercicio — uma por ano).</summary>
    /// <param name="exercicio">Exercicio orcamentario.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A apuracao do exercicio, ou <c>null</c>.</returns>
    Task<ApuracaoArt29A?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken);

    /// <summary>Verifica se ja existe apuracao para o exercicio no tenant (unicidade logica).</summary>
    /// <param name="exercicio">Exercicio orcamentario.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existe apuracao do exercicio.</returns>
    Task<bool> ExisteParaExercicioAsync(int exercicio, CancellationToken cancellationToken);
}
