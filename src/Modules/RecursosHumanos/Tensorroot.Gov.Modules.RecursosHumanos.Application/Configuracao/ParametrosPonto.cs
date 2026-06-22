namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

/// <summary>
/// Parametros parametrizaveis por tenant do ponto eletronico (Portaria MTP 671/2021), nunca
/// <em>hardcoded</em> (CLAUDE.md §7): identificacao do empregador para o cabecalho AFD/AEJ, origem
/// padrao de marcacao (tipo de REP) e janela do banco de horas. A APLICABILIDADE da 671 ao
/// estatutario e // TODO(validar-oficial) — definida por <see cref="AplicarPortaria671AoEstatutario"/>.
/// </summary>
public sealed class ParametrosPonto
{
    /// <summary>Secao de configuracao.</summary>
    public const string SecaoConfiguracao = "RecursosHumanos:Ponto";

    /// <summary>CNPJ do ente (empregador) para o cabecalho do AFD/AEJ (so digitos).</summary>
    public string? CnpjEnte { get; init; }

    /// <summary>Razao social/nome do ente para o cabecalho do AFD/AEJ.</summary>
    public string? RazaoSocialEnte { get; init; }

    /// <summary>Origem padrao da marcacao quando nao informada (1=REP-C, 2=REP-A, 3=REP-P). Padrao: REP-P.</summary>
    public int OrigemPadrao { get; init; } = 3;

    /// <summary>
    /// Janela do banco de horas em meses: 6 (acordo individual) ou 12 (ACT/CCT) — Portaria 671.
    /// // TODO(validar-oficial): janela e regra de zeragem para o estatutario (lei municipal/RJU).
    /// </summary>
    public int BancoHorasJanelaMeses { get; init; } = 6;

    /// <summary>
    /// Decisao parametrizavel do ente: aplicar a Portaria 671 (AFD/AEJ/REP) tambem aos estatutarios.
    /// Default <c>false</c> — a 671 regulamenta a CLT; o estatutario segue lei municipal/RJU/TCE-RS.
    /// // TODO(validar-oficial): confirmar no RJU de Maximiliano de Almeida/RS e normas do TCE-RS.
    /// </summary>
    public bool AplicarPortaria671AoEstatutario { get; init; }
}
