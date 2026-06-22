using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;

/// <summary>
/// Monta o <see cref="AgregadorDemonstrativo"/> de uma competência: carrega o balancete e o mapa de
/// atributos (indicador F/P por conta) necessários para agregar as linhas dos demonstrativos DCASP.
/// </summary>
public sealed class DemonstrativoContexto(IBalanceteProjection balancete, IContaContabilRepository contas)
{
    /// <summary>Carrega o agregador da competência.</summary>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="mes">Mês.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Agregador pronto para somar linhas sobre o balancete.</returns>
    public async Task<AgregadorDemonstrativo> CarregarAsync(
        int exercicio,
        int mes,
        CancellationToken cancellationToken)
    {
        var linhas = await balancete.ListarPorPeriodoAsync(exercicio, mes, cancellationToken).ConfigureAwait(false);
        var todasContas = await contas.ListarTodasAsync(cancellationToken).ConfigureAwait(false);

        var indicadorPorConta = todasContas.ToDictionary(
            c => c.Codigo.Codigo,
            c => c.IndicadorSuperavitFinanceiro,
            StringComparer.Ordinal);

        return new AgregadorDemonstrativo(linhas, indicadorPorConta);
    }
}
