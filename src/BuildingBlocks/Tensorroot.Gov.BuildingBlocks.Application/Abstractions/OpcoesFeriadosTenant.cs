namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Parametros de feriados POR TENANT lidos da configuracao (secao <see cref="SecaoConfiguracao"/>).
/// Os feriados nacionais FIXOS e MOVEIS sao derivados por lei/algoritmo (nao configuraveis); aqui ficam
/// apenas as escolhas locais do ente: adocao de pontos facultativos e feriados municipais. Defaults
/// documentais espelham o piso legal; valores reais vem da config do tenant (CLAUDE.md S7/S16).
/// Evolucao natural (sem mudar o contrato): tabela <c>core.CalendarioFeriado</c> por tenant.
/// </summary>
public sealed class OpcoesFeriadosTenant
{
    /// <summary>
    /// Secao RAIZ de feriados. Mantida como FALLBACK do ente unico (piloto single-tenant): quando o
    /// tenant atual NAO possui sub-secao propria, vale o que estiver aqui. NAO deve ser usada como fonte
    /// direta em producao multi-tenant — preferir <see cref="SecaoConfiguracaoDoTenant"/>.
    /// </summary>
    public const string SecaoConfiguracao = "Tempo:Feriados";

    /// <summary>
    /// Sub-secao de feriados POR TENANT. Cada ente tem suas escolhas locais isoladas pelo proprio
    /// <c>TenantId</c> em <c>Tempo:Feriados:Tenants:{tenantId}</c> — diferentes municipios-tenant nao
    /// compartilham mais o mesmo conjunto municipal. Evolucao natural (sem mudar o contrato): tabela
    /// <c>core.CalendarioFeriado</c> por tenant.
    /// </summary>
    public const string SubSecaoTenants = "Tenants";

    /// <summary>
    /// Caminho da sub-secao de configuracao do <paramref name="tenantId"/>
    /// (<c>Tempo:Feriados:Tenants:{tenantId}</c>). Isola as escolhas municipais/facultativas por tenant.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant atual.</param>
    /// <returns>Caminho ConfigurationSection da sub-secao do tenant.</returns>
    public static string SecaoConfiguracaoDoTenant(Guid tenantId)
        => $"{SecaoConfiguracao}:{SubSecaoTenants}:{tenantId:D}";

    /// <summary>Se o tenant adota o Carnaval (segunda e terca) como dia nao util. Default: <c>false</c>.</summary>
    public bool AdotaCarnaval { get; init; }

    /// <summary>Se o tenant adota a Quarta-feira de Cinzas como dia nao util. Default: <c>false</c>.</summary>
    public bool AdotaQuartaCinzas { get; init; }

    /// <summary>Se o tenant adota Corpus Christi como dia nao util. Default: <c>false</c>.</summary>
    public bool AdotaCorpusChristi { get; init; }

    /// <summary>
    /// Feriados municipais RECORRENTES em formato "MM-dd" (ex.: aniversario do municipio, padroeiro).
    /// Aplicados todo ano.
    /// </summary>
    public IReadOnlyList<string> MunicipaisFixos { get; init; } = [];

    /// <summary>
    /// Feriados/pontos municipais PONTUAIS em formato "yyyy-MM-dd" (datas decretadas para um ano especifico).
    /// </summary>
    public IReadOnlyList<string> MunicipaisData { get; init; } = [];
}
