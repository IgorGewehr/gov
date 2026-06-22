namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

/// <summary>
/// Natureza da informação contábil (PCASP §3 / MCASP). Determina o universo de partidas:
/// débito e crédito de um mesmo lançamento DEVEM pertencer à mesma natureza.
/// </summary>
public enum NaturezaInformacao
{
    /// <summary>Patrimonial — classes 1 a 4 (Ativo, Passivo/PL, VPD, VPA).</summary>
    Patrimonial = 1,

    /// <summary>Orçamentária — classes 5 e 6 (aprovação e execução do orçamento).</summary>
    Orcamentaria = 2,

    /// <summary>Controle — classes 7 e 8 (atos potenciais, riscos, garantias).</summary>
    Controle = 3,
}

/// <summary>Natureza do saldo da conta (PCASP §1/§2).</summary>
public enum NaturezaSaldo
{
    /// <summary>Devedora — classes 1, 3, 5, 7.</summary>
    Devedora = 1,

    /// <summary>Credora — classes 2, 4, 6, 8.</summary>
    Credora = 2,

    /// <summary>Híbrida/Mista — contas redutoras e similares (atributo "Híbrida" do §1).</summary>
    Mista = 3,
}

/// <summary>Tipo da conta quanto à possibilidade de lançamento.</summary>
public enum TipoConta
{
    /// <summary>Sintética — agrega filhas, não recebe lançamento direto.</summary>
    Sintetica = 1,

    /// <summary>Analítica — conta folha, recebe partidas.</summary>
    Analitica = 2,
}

/// <summary>
/// Indicador de superávit financeiro (art. 105, Lei 4.320/1964): classifica Ativo/Passivo
/// como Financeiro (F) ou Permanente (P). Obrigatório apenas em contas das classes 1 e 2.
/// </summary>
public enum IndicadorSuperavitFinanceiro
{
    /// <summary>Não aplicável (contas que não são Ativo/Passivo).</summary>
    NaoAplicavel = 0,

    /// <summary>Financeiro (F).</summary>
    Financeiro = 1,

    /// <summary>Permanente (P).</summary>
    Permanente = 2,
}
