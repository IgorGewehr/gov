using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Queries;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>
/// Consultas analíticas sobre os lançamentos contábeis (read-side): razão lançamento-a-lançamento
/// e diário cronológico. Separado do repositório de escrita para não vazar EF na Application.
/// </summary>
public interface ILancamentoContabilConsulta
{
    /// <summary>Razão analítico de uma conta (partidas em ordem, com saldo acumulado).</summary>
    /// <param name="contaId">Conta.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Linhas analíticas.</returns>
    Task<IReadOnlyList<LinhaRazaoAnaliticoDto>> RazaoAnaliticoAsync(Guid contaId, int exercicio, CancellationToken cancellationToken);

    /// <summary>Livro Diário: lançamentos cronológicos no intervalo.</summary>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="de">Data inicial (opcional).</param>
    /// <param name="ate">Data final (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Linhas do diário.</returns>
    Task<IReadOnlyList<LinhaDiarioDto>> DiarioAsync(int exercicio, DateOnly? de, DateOnly? ate, CancellationToken cancellationToken);
}
