using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;

/// <summary>
/// Agrega saldos do balancete para compor as linhas de um demonstrativo DCASP: soma o <c>SaldoAtual</c>
/// das contas analíticas sob um prefixo PCASP, opcionalmente filtrando pelo indicador F/P (Financeiro/
/// Permanente). Trabalha sobre o balancete (read-model) + um dicionário de atributos de conta por código.
/// </summary>
public sealed class AgregadorDemonstrativo
{
    private readonly IReadOnlyList<LinhaBalancete> _balancete;
    private readonly IReadOnlyDictionary<string, IndicadorSuperavitFinanceiro> _indicadorPorConta;

    /// <summary>Cria o agregador para uma competência.</summary>
    /// <param name="balancete">Linhas do balancete da competência.</param>
    /// <param name="indicadorPorConta">Mapa código PCASP → indicador F/P da conta.</param>
    public AgregadorDemonstrativo(
        IReadOnlyList<LinhaBalancete> balancete,
        IReadOnlyDictionary<string, IndicadorSuperavitFinanceiro> indicadorPorConta)
    {
        ArgumentNullException.ThrowIfNull(balancete);
        ArgumentNullException.ThrowIfNull(indicadorPorConta);
        _balancete = balancete;
        _indicadorPorConta = indicadorPorConta;
    }

    /// <summary>
    /// Soma os saldos atuais das contas sob o prefixo, aplicando o filtro F/P. O sinal segue a natureza
    /// do saldo (já refletido no <c>SaldoAtual</c> do balancete).
    /// </summary>
    /// <param name="prefixo">Prefixo PCASP (ex.: "1.1", "6.2.1.2").</param>
    /// <param name="filtro">Filtro por indicador F/P.</param>
    /// <returns>Soma dos saldos.</returns>
    public decimal Somar(string prefixo, FiltroSuperavit filtro = FiltroSuperavit.Qualquer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefixo);

        return _balancete
            .Where(l => EstaSob(l.CodigoConta, prefixo) && AtendeFiltro(l.CodigoConta, filtro))
            .Sum(l => l.SaldoAtual);
    }

    /// <summary>Soma a partir de um mapa de linha (prefixo + filtro).</summary>
    /// <param name="mapa">Mapa da linha do demonstrativo.</param>
    /// <returns>Soma dos saldos da linha.</returns>
    public decimal Somar(MapaLinhaDemonstrativo mapa)
    {
        ArgumentNullException.ThrowIfNull(mapa);
        return Somar(mapa.PrefixoConta, mapa.FiltroSuperavit);
    }

    // Comparação por prefixo de string idêntica à CodigoContabil.EstaSob (igualdade ou subárvore).
    private static bool EstaSob(string codigo, string prefixo)
        => codigo == prefixo || codigo.StartsWith(prefixo + ".", StringComparison.Ordinal);

    private bool AtendeFiltro(string codigo, FiltroSuperavit filtro)
    {
        if (filtro == FiltroSuperavit.Qualquer)
        {
            return true;
        }

        if (!_indicadorPorConta.TryGetValue(codigo, out var indicador))
        {
            return false;
        }

        return filtro switch
        {
            FiltroSuperavit.Financeiro => indicador == IndicadorSuperavitFinanceiro.Financeiro,
            FiltroSuperavit.Permanente => indicador == IndicadorSuperavitFinanceiro.Permanente,
            _ => true,
        };
    }
}
