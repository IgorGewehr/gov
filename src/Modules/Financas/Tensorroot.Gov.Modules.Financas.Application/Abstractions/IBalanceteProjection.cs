using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>
/// Projeção do balancete: aplica partidas a linhas (conta, exercício, mês) e consulta saldos.
/// Implementada na Infrastructure sobre a tabela <c>financas.balancete_conta</c>.
/// </summary>
public interface IBalanceteProjection
{
    /// <summary>
    /// Obtém (ou cria) a linha de balancete de uma conta em um período, herdando o saldo anterior
    /// do mês precedente quando a linha ainda não existe.
    /// </summary>
    /// <param name="conta">Conta analítica.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="periodoMes">Mês (1-12).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A linha de balancete (rastreada).</returns>
    Task<LinhaBalancete> ObterOuCriarLinhaAsync(
        ContaContabil conta,
        int exercicio,
        int periodoMes,
        CancellationToken cancellationToken);

    /// <summary>Lista as linhas do balancete de um período.</summary>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="periodoMes">Mês (1-12).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Linhas do balancete.</returns>
    Task<IReadOnlyList<LinhaBalancete>> ListarPorPeriodoAsync(
        int exercicio,
        int periodoMes,
        CancellationToken cancellationToken);

    /// <summary>Lista o razão (linhas mensais) de uma conta num exercício.</summary>
    /// <param name="contaId">Conta.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Linhas mensais da conta.</returns>
    Task<IReadOnlyList<LinhaBalancete>> ListarRazaoContaAsync(
        Guid contaId,
        int exercicio,
        CancellationToken cancellationToken);
}
