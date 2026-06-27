namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;

/// <summary>
/// Parametros previdenciarios do ente, lidos da configuracao (Key Vault/IOptions), NUNCA hardcoded
/// (CLAUDE.md §7). Determinam o vinculo previdenciario do quadro (RPPS proprio x RGPS/INSS) e, com ele,
/// o motor de contribuicao e o evento eSocial (S-1200 RGPS x S-1202 RPPS).
/// </summary>
public sealed class ParametrosPrevidencia
{
    /// <summary>Secao de configuracao (<c>RecursosHumanos:Previdencia</c>).</summary>
    public const string SecaoConfiguracao = "RecursosHumanos:Previdencia";

    /// <summary>
    /// Verdadeiro se o ente possui RPPS proprio instituido por lei municipal (EC 103/2019). Default
    /// <c>false</c> (RGPS/INSS para todo o quadro) — caso de municipios pequenos sem RPPS proprio, como
    /// Maximiliano de Almeida (~5 mil hab.). CONFIRMADO PELO DONO: o ente piloto e INSS puro (sem RPPS
    /// proprio), de modo que o default <c>false</c> e tambem o valor de producao do tenant piloto. A
    /// opcao RPPS permanece parametrizavel para outros tenants que instituam RPPS por lei municipal.
    /// </summary>
    public bool PossuiRppsProprio { get; set; }
}
