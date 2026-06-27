namespace Tensorroot.Gov.Modules.Patrimonio.Application;

/// <summary>
/// Parâmetros de gestão de frota configuráveis por tenant (CLAUDE.md §7/§16: sem número mágico no
/// código). Os DEFAULTS espelham referências usuais (sulco mínimo legal de 1,6 mm — CONTRAN/CTB; janelas
/// de alerta de 30 dias para vencimentos de CNH e apólice), mas TODOS são sobrescritíveis por
/// configuração do tenant. Nenhum desses valores vive literal nos agregados/handlers.
/// </summary>
public sealed class FrotaOptions
{
    /// <summary>Seção de configuração (por tenant).</summary>
    public const string SecaoConfig = "Patrimonio:Frota";

    /// <summary>
    /// Sulco mínimo legal de circulação, em milímetros (default: 1,6 mm — CONTRAN/CTB). Abaixo (ou igual)
    /// disso o pneu deve ser removido de rodagem (recapagem ou descarte).
    /// </summary>
    public decimal SulcoMinimoLegalMilimetros { get; set; } = 1.6m;

    /// <summary>Janela padrão (em dias) do alerta de CNH a vencer (default: 30 dias).</summary>
    public int AlertaCnhVencendoDias { get; set; } = 30;

    /// <summary>Janela padrão (em dias) do alerta de apólice de seguro a vencer (default: 30 dias).</summary>
    public int AlertaApoliceVencendoDias { get; set; } = 30;
}
