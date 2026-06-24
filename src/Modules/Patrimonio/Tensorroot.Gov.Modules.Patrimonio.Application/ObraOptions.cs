using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Patrimonio.Application;

/// <summary>
/// Parâmetros de prazo de obras configuráveis por tenant (Lei 14.133/2021, art. 94 §3). Os DEFAULTS
/// espelham a lei (25 d.u. após assinatura; 45 d.u. após conclusão; alerta 5 d.u. de antecedência), mas
/// TODOS são sobrescritíveis por configuração do tenant (CLAUDE.md §7/§16) — o número nunca vive no código
/// do agregado. Mapeado em <c>IParametrosObraProvider</c> na Infrastructure.
/// </summary>
public sealed class ObraOptions
{
    /// <summary>Seção de configuração (por tenant).</summary>
    public const string SecaoConfig = "Patrimonio:Obras";

    /// <summary>Quantidade do prazo de publicação/registro após a ASSINATURA (default legal: 25).</summary>
    public int AposAssinaturaQuantidade { get; set; } = 25;

    /// <summary>Unidade do prazo após a assinatura (default: dias úteis).</summary>
    public UnidadePrazo AposAssinaturaUnidade { get; set; } = UnidadePrazo.DiasUteis;

    /// <summary>Norma-fonte do prazo após a assinatura.</summary>
    public string AposAssinaturaNormaFonte { get; set; } = "Lei 14.133/2021 art. 94 §3";

    /// <summary>Quantidade do prazo de publicação/registro após a CONCLUSÃO (default legal: 45).</summary>
    public int AposConclusaoQuantidade { get; set; } = 45;

    /// <summary>Unidade do prazo após a conclusão (default: dias úteis).</summary>
    public UnidadePrazo AposConclusaoUnidade { get; set; } = UnidadePrazo.DiasUteis;

    /// <summary>Norma-fonte do prazo após a conclusão.</summary>
    public string AposConclusaoNormaFonte { get; set; } = "Lei 14.133/2021 art. 94 §3";

    /// <summary>Antecedência (em dias úteis) do alerta de prazo a vencer ao Portal do Gestor (default: 5).</summary>
    public int AntecedenciaAlertaDiasUteis { get; set; } = 5;
}
