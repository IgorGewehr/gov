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
    /// Maximiliano de Almeida (~5 mil hab.). // TODO(confirmar-dono): confirmar Sim/Nao para o ente.
    /// </summary>
    public bool PossuiRppsProprio { get; set; }
}
