namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

/// <summary>
/// Parametros parametrizaveis por tenant da folha de pagamento (nunca <em>hardcoded</em> — CLAUDE.md S7):
/// teto remuneratorio constitucional (CF art. 37, XI), rubrica de abate-teto e prazo de envio periodico.
/// </summary>
public sealed class ParametrosFolha
{
    /// <summary>Secao de configuracao.</summary>
    public const string SecaoConfiguracao = "RecursosHumanos:Folha";

    /// <summary>Teto remuneratorio constitucional aplicado no abate-teto (CF art. 37, XI).</summary>
    public decimal TetoRemuneratorio { get; init; }

    /// <summary>Codigo da rubrica de abate-teto (S-1010); por padrao <see cref="Domain.Folha.FolhaDePagamento.CodigoRubricaAbateTetoPadrao"/>.</summary>
    public string? CodigoRubricaAbateTeto { get; init; }

    /// <summary>Dia-limite (do mes seguinte a competencia) para envio dos eventos periodicos (I-13). Padrao: 15.</summary>
    public int DiaLimiteEnvioPeriodico { get; init; } = 15;

    /// <summary>
    /// Limiar (|Delta| em R$) da divergencia de VARIACAO do liquido por servidor na conferencia de
    /// pre-fechamento (P0-7): quando o liquido de um servidor varia, em modulo, acima deste valor frente
    /// a competencia anterior, a folha sinaliza para o conferente (pega erro de digitacao/rubrica).
    /// Parametrizavel por tenant; a query aceita sobrescrita pontual. Padrao: R$ 1.000,00.
    /// </summary>
    public decimal LimiteVariacaoLiquidoConferencia { get; init; } = 1_000m;

    /// <summary>Codigo da rubrica de desconto de INSS apurada pelo motor (parametrizavel por tenant). Padrao: INSS.</summary>
    public string CodigoRubricaInss { get; init; } = "INSS";

    /// <summary>Codigo da rubrica de desconto de RPPS apurada pelo motor (parametrizavel por tenant). Padrao: RPPS.</summary>
    public string CodigoRubricaRpps { get; init; } = "RPPS";

    /// <summary>Codigo da rubrica de desconto de IRRF apurada pelo motor (parametrizavel por tenant). Padrao: IRRF.</summary>
    public string CodigoRubricaIrrf { get; init; } = "IRRF";

    // --- Ciclo anual (13o, ferias, rescisao) — codigos de rubrica e fracoes parametrizaveis (design §1.4).
    // Todos sao DADO sobrescrevivel por tenant; defaults documentais espelham o padrao das rubricas legais.

    /// <summary>Codigo da rubrica de provento do 13o salario. Padrao: 13-SAL.</summary>
    public string CodigoRubrica13Salario { get; init; } = "13-SAL";

    /// <summary>Codigo da rubrica de INSS PROPRIO do 13o (base separada). Padrao: INSS-13.</summary>
    public string CodigoRubricaInss13 { get; init; } = "INSS-13";

    /// <summary>Codigo da rubrica de RPPS PROPRIO do 13o (base separada). Padrao: RPPS-13.</summary>
    public string CodigoRubricaRpps13 { get; init; } = "RPPS-13";

    /// <summary>Codigo da rubrica de IRRF PROPRIO do 13o (base separada, exclusivo na fonte). Padrao: IRRF-13.</summary>
    public string CodigoRubricaIrrf13 { get; init; } = "IRRF-13";

    /// <summary>Codigo da rubrica informativa-dedutora da 1a parcela do 13o ja paga (abatida na 2a). Padrao: 13-ADIANT.</summary>
    public string CodigoRubrica13Adiantamento { get; init; } = "13-ADIANT";

    /// <summary>Percentual (fracao) da 1a parcela do 13o (CLT/Lei 4.749/65 art. 2). Padrao: 0,5.</summary>
    public decimal PercentualPrimeiraParcela13 { get; init; } = 0.5m;

    /// <summary>Codigo da rubrica de provento de ferias. Padrao: FERIAS.</summary>
    public string CodigoRubricaFerias { get; init; } = "FERIAS";

    /// <summary>Codigo da rubrica do 1/3 constitucional de ferias. Padrao: 1/3-FERIAS.</summary>
    public string CodigoRubricaTercoFerias { get; init; } = "1/3-FERIAS";

    /// <summary>Codigo da rubrica do abono pecuniario (venda de ate 1/3). Padrao: ABONO-PEC.</summary>
    public string CodigoRubricaAbonoPecuniario { get; init; } = "ABONO-PEC";

    /// <summary>Codigo da rubrica do 1/3 sobre o abono pecuniario. Padrao: 1/3-ABONO.</summary>
    public string CodigoRubricaTercoAbono { get; init; } = "1/3-ABONO";

    /// <summary>Fracao do terco constitucional de ferias (CF art. 7 XVII; minimo 1/3). Padrao: 1/3.</summary>
    public decimal FracaoTercoConstitucional { get; init; } = 1m / 3m;

    /// <summary>Codigo da rubrica de saldo de salario (rescisao). Padrao: SALDO-SAL.</summary>
    public string CodigoRubricaSaldoSalario { get; init; } = "SALDO-SAL";

    /// <summary>Codigo da rubrica de 13o proporcional (rescisao). Padrao: 13-PROP.</summary>
    public string CodigoRubrica13Proporcional { get; init; } = "13-PROP";

    /// <summary>Codigo da rubrica de ferias vencidas + 1/3 indenizadas (rescisao). Padrao: FERIAS-VENC.</summary>
    public string CodigoRubricaFeriasVencidas { get; init; } = "FERIAS-VENC";

    /// <summary>Codigo da rubrica de ferias proporcionais + 1/3 indenizadas (rescisao). Padrao: FERIAS-PROP.</summary>
    public string CodigoRubricaFeriasProporcionais { get; init; } = "FERIAS-PROP";

    /// <summary>Codigo da rubrica de aviso previo (so celetista). Padrao: AVISO-PREV.</summary>
    public string CodigoRubricaAvisoPrevio { get; init; } = "AVISO-PREV";

    /// <summary>Codigo da rubrica de multa de 40% do FGTS (so celetista). Padrao: MULTA-FGTS.</summary>
    public string CodigoRubricaMultaFgts { get; init; } = "MULTA-FGTS";

    /// <summary>Codigo da rubrica de desconto da pensao alimenticia (retida do servidor). Padrao: PENSAO-ALIM.</summary>
    public string CodigoRubricaPensaoAlimenticia { get; init; } = "PENSAO-ALIM";
}
