namespace Tensorroot.Gov.Modules.Financas.Application.RestosAPagar;

/// <summary>
/// Opções de prazo (decadência) para cancelamento de Restos a Pagar, parametrizáveis por
/// tenant (CLAUDE.md: nada hardcoded). Refletem o Decreto 93.872/86 e as normas locais
/// do TCE-RS. Os prazos são expressos em <b>exercícios de validade após a inscrição</b>.
/// </summary>
public sealed class OpcoesRestosAPagar
{
    /// <summary>Seção de configuração.</summary>
    public const string SecaoConfig = "Financas:RestosAPagar";

    /// <summary>
    /// Validade, em exercícios após a inscrição, de um RAP <b>Não Processado</b> antes da
    /// decadência (Decreto 93.872/86: prescrição ao fim do exercício seguinte ao da inscrição).
    /// Default conservador.
    /// </summary>
    // TODO(validar-oficial): janela vigente do TCE-RS para RAP Nao Processado (porte do piloto).
    public int ValidadeExerciciosNaoProcessado { get; set; } = 1;

    /// <summary>
    /// Validade, em exercícios após a inscrição, de um RAP <b>Processado</b> antes da
    /// decadência. Default conservador.
    /// </summary>
    // TODO(validar-oficial): janela vigente do TCE-RS para RAP Processado (porte do piloto).
    public int ValidadeExerciciosProcessado { get; set; } = 1;
}
