using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Application;

/// <summary>
/// Parametros do PNCP configuraveis por tenant (Lei 14.133/2021, art. 94). Os DEFAULTS espelham a lei
/// (divulgacao 20 d.u.; extrato 10 d.u.; alerta 5 d.u. de antecedencia), mas TODOS sao sobrescrititiveis
/// por configuracao do tenant (CLAUDE.md §7/§16) — o numero nunca vive no codigo do agregado. Mapeado em
/// <c>IPncpParametros</c> na Infrastructure.
/// </summary>
public sealed class PncpOptions
{
    /// <summary>Secao de configuracao (por tenant).</summary>
    public const string SecaoConfig = "Administracao:Pncp";

    /// <summary>Quantidade do prazo de DIVULGACAO do contrato no PNCP (default legal: 20).</summary>
    public int DivulgacaoQuantidade { get; set; } = 20;

    /// <summary>Unidade do prazo de divulgacao (default: dias uteis).</summary>
    public UnidadePrazo DivulgacaoUnidade { get; set; } = UnidadePrazo.DiasUteis;

    /// <summary>Norma-fonte do prazo de divulgacao.</summary>
    public string DivulgacaoNormaFonte { get; set; } = "Lei 14.133/2021 art. 94";

    /// <summary>Quantidade do prazo de divulgacao do EXTRATO do contrato (default legal: 10).</summary>
    public int RegistroExtratoQuantidade { get; set; } = 10;

    /// <summary>Unidade do prazo do extrato (default: dias uteis).</summary>
    public UnidadePrazo RegistroExtratoUnidade { get; set; } = UnidadePrazo.DiasUteis;

    /// <summary>Norma-fonte do prazo do extrato.</summary>
    public string RegistroExtratoNormaFonte { get; set; } = "Lei 14.133/2021 art. 94";

    /// <summary>Antecedencia (em dias uteis) do alerta de prazo a vencer ao Portal do Gestor (default: 5).</summary>
    public int AntecedenciaAlertaDiasUteis { get; set; } = 5;
}
