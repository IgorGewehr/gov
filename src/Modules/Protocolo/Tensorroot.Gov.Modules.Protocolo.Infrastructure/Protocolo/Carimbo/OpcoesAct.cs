namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo.Carimbo;

/// <summary>
/// Opcoes da Autoridade de Carimbo do Tempo (ACT) por tenant (RFC 3161 / DOC-ICP-12 v2.1).
/// Endpoint e credencial vivem no Azure Key Vault (M2) — NUNCA no repositorio (CLAUDE.md S5).
/// </summary>
public sealed class OpcoesAct
{
    /// <summary>Secao de configuracao.</summary>
    public const string Secao = "Protocolo:Act";

    /// <summary>Nome da ACT credenciada (exibido no carimbo).</summary>
    public string Autoridade { get; set; } = "Tensorroot.Gov ACT (stub M9)";

    /// <summary>Endpoint TSP da ACT (Key Vault em producao). Vazio em dev/stub.</summary>
    public string? Endpoint { get; set; }

    /// <summary>OID da politica de carimbo esperada (TSA policy ICP-Brasil).</summary>
    public string PoliticaEsperada { get; set; } = "2.16.76.1.6.999";

    /// <summary>Thumbprints das AC do tempo ICP-Brasil confiaveis (validacao da cadeia).</summary>
    public IList<string> EmissoresConfiaveis { get; } = [];

    /// <summary>Tolerancia (segundos) do genTime em relacao ao instante atual.</summary>
    public int ToleranciaGenTimeSegundos { get; set; } = 300;

    /// <summary>Timeout (segundos) da chamada HTTP a ACT.</summary>
    public int TimeoutSegundos { get; set; } = 15;
}
