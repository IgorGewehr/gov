using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;

/// <summary>Demonstrativo DCASP do núcleo M3.</summary>
public enum Demonstrativo
{
    /// <summary>Balanço Orçamentário (Anexo 12) — classes 5 e 6.</summary>
    BalancoOrcamentario = 1,

    /// <summary>Balanço Financeiro (Anexo 13).</summary>
    BalancoFinanceiro = 2,

    /// <summary>Balanço Patrimonial (Anexo 14) — classes 1, 2, 7/8.</summary>
    BalancoPatrimonial = 3,

    /// <summary>Demonstração das Variações Patrimoniais (Anexo 15) — classes 3 e 4.</summary>
    DemonstracaoVariacoesPatrimoniais = 4,
}

/// <summary>Filtro de atributo de superávit financeiro aplicado a uma linha de demonstrativo.</summary>
public enum FiltroSuperavit
{
    /// <summary>Sem filtro por indicador F/P.</summary>
    Qualquer = 0,

    /// <summary>Apenas contas Financeiras (F).</summary>
    Financeiro = 1,

    /// <summary>Apenas contas Permanentes (P).</summary>
    Permanente = 2,
}

/// <summary>
/// Mapa parametrizável conta→linha de um demonstrativo (DCASP §9): cada linha de quadro é a agregação dos
/// saldos das contas analíticas sob um prefixo PCASP, opcionalmente filtradas por indicador F/P. O mapa é
/// por exercício (segue a versão PCASP/MCASP) e NUNCA hardcoded em código de cálculo — o cálculo apenas
/// aplica este mapa sobre o balancete. [validar-oficial]: layouts linha-a-linha dos Anexos 12–15.
/// </summary>
/// <param name="Exercicio">Exercício de vigência do mapa.</param>
/// <param name="Demonstrativo">Demonstrativo a que a linha pertence.</param>
/// <param name="Quadro">Quadro do demonstrativo (ex.: "Receitas", "Ativo").</param>
/// <param name="Ordem">Ordem de exibição da linha no quadro.</param>
/// <param name="Linha">Rótulo da linha.</param>
/// <param name="PrefixoConta">Prefixo PCASP cujas contas analíticas somam nesta linha.</param>
/// <param name="FiltroSuperavit">Filtro por indicador F/P (Financeiro/Permanente), se aplicável.</param>
public sealed record MapaLinhaDemonstrativo(
    int Exercicio,
    Demonstrativo Demonstrativo,
    string Quadro,
    int Ordem,
    string Linha,
    string PrefixoConta,
    FiltroSuperavit FiltroSuperavit = FiltroSuperavit.Qualquer);
