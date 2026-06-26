namespace Tensorroot.Gov.Modules.Administracao.Application;

/// <summary>
/// Parametros da DISPENSA em razao do valor (Lei 14.133/2021, art. 75, I e II), configuraveis por
/// tenant. Os DEFAULTS espelham o Dec. 12.807/2025 (vigencia 01/01/2026, que REVOGOU o Dec. 12.343/2024
/// — art. 4º), mas TODOS sao sobrescrititiveis por configuracao do tenant (CLAUDE.md §7/§16) — os valores
/// sao atualizados anualmente em 1 de janeiro pelo IPCA-E (art. 182), entao NUNCA vivem no codigo do
/// agregado. O prazo minimo de divulgacao do aviso de contratacao direta (IN SEGES/ME 67/2021; art. 75 §3)
/// tambem e parametrizavel.
/// </summary>
public sealed class DispensaOptions
{
    /// <summary>Secao de configuracao (por tenant).</summary>
    public const string SecaoConfig = "Administracao:Dispensa";

    /// <summary>
    /// Limite de dispensa para obras e servicos de engenharia/manutencao de veiculos (art. 75, I).
    /// Default: Dec. 12.807/2025, vigencia 01/01/2026 (R$ 130.984,20).
    /// </summary>
    public decimal LimiteObrasEngenharia { get; set; } = 130_984.20m;

    /// <summary>
    /// Limite de dispensa para outros servicos e compras (art. 75, II).
    /// Default: Dec. 12.807/2025, vigencia 01/01/2026 (R$ 65.492,11).
    /// </summary>
    public decimal LimiteOutrosServicosCompras { get; set; } = 65_492.11m;

    /// <summary>Norma-fonte dos limites aplicados (rastreabilidade da parametrizacao).</summary>
    public string LimiteNormaFonte { get; set; } = "Lei 14.133/2021 art. 75, I/II; Dec. 12.807/2025 (vigencia 2026)";

    /// <summary>
    /// Prazo minimo de divulgacao do aviso de contratacao direta antes da abertura da disputa
    /// (IN SEGES/ME 67/2021): default 3 dias uteis.
    /// </summary>
    public int PrazoMinimoDivulgacaoDiasUteis { get; set; } = 3;
}
