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

    /// <summary>Codigo da rubrica de desconto de INSS apurada pelo motor (parametrizavel por tenant). Padrao: INSS.</summary>
    public string CodigoRubricaInss { get; init; } = "INSS";

    /// <summary>Codigo da rubrica de desconto de RPPS apurada pelo motor (parametrizavel por tenant). Padrao: RPPS.</summary>
    public string CodigoRubricaRpps { get; init; } = "RPPS";

    /// <summary>Codigo da rubrica de desconto de IRRF apurada pelo motor (parametrizavel por tenant). Padrao: IRRF.</summary>
    public string CodigoRubricaIrrf { get; init; } = "IRRF";
}
