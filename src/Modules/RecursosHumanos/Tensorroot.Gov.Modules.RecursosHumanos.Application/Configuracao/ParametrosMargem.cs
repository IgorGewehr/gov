namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

/// <summary>
/// Percentuais LEGAIS de margem consignavel parametrizaveis por tenant (nunca <em>hardcoded</em> —
/// CLAUDE.md S7/S16). Servem de DEFAULT quando o tenant nao cadastrou um <c>ParametrosMargemVigente</c>
/// persistido: o provider materializa os percentuais a partir destes valores. Os defaults documentais
/// espelham a Lei 14.131/2021 (35% geral + 5% cartao consignado + 5% cartao beneficio = 45% total). O ente
/// sobrescreve por lei municipal. Tambem expoe o codigo da rubrica de desconto consignado na folha.
/// </summary>
public sealed class ParametrosMargem
{
    /// <summary>Secao de configuracao.</summary>
    public const string SecaoConfiguracao = "RecursosHumanos:Margem";

    /// <summary>Fracao (0..1) da margem GERAL. Padrao legal 0,35 (Lei 14.131/2021).</summary>
    public decimal PercentualGeral { get; init; } = 0.35m;

    /// <summary>Fracao (0..1) da reserva de CARTAO DE CREDITO consignado. Padrao legal 0,05.</summary>
    public decimal PercentualCartaoConsignado { get; init; } = 0.05m;

    /// <summary>Fracao (0..1) da reserva de CARTAO BENEFICIO. Padrao legal 0,05.</summary>
    public decimal PercentualCartaoBeneficio { get; init; } = 0.05m;
}
