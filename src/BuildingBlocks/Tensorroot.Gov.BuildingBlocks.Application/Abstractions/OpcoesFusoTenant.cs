namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Parametro de FUSO HORARIO por tenant lido da configuracao (secao <see cref="SecaoConfiguracao"/>).
/// Default <see cref="FusoPadrao"/> (<c>America/Sao_Paulo</c>) — sem numero magico, constante nomeada
/// (CLAUDE.md S7/S16). Cada municipio-tenant pode declarar seu proprio fuso na sub-secao do PROPRIO
/// tenant, com fallback para a secao raiz do ente unico (piloto single-tenant). Espelha a mecanica de
/// <see cref="OpcoesFeriadosTenant"/>. Evolucao natural (sem mudar o contrato): coluna na tabela de
/// parametros do tenant.
/// </summary>
public sealed class OpcoesFusoTenant
{
    /// <summary>
    /// Fuso default do Brasil continental (Horario de Brasilia, UTC-3). Identificador IANA — o
    /// <c>TimeZoneInfo.FindSystemTimeZoneById</c> do .NET 8 resolve IANA em todas as plataformas.
    /// </summary>
    public const string FusoPadrao = "America/Sao_Paulo";

    /// <summary>
    /// Secao RAIZ de fuso. FALLBACK do ente unico (piloto single-tenant): quando o tenant atual NAO
    /// possui sub-secao propria, vale o que estiver aqui. Em producao multi-tenant, preferir
    /// <see cref="SecaoConfiguracaoDoTenant"/>.
    /// </summary>
    public const string SecaoConfiguracao = "Tempo:Fuso";

    /// <summary>Sub-secao por tenant (<c>Tempo:Fuso:Tenants:{tenantId}</c>). Isola o fuso por tenant.</summary>
    public const string SubSecaoTenants = "Tenants";

    /// <summary>
    /// Caminho da sub-secao de configuracao do <paramref name="tenantId"/>
    /// (<c>Tempo:Fuso:Tenants:{tenantId}</c>).
    /// </summary>
    /// <param name="tenantId">Identificador do tenant atual.</param>
    /// <returns>Caminho ConfigurationSection da sub-secao do tenant.</returns>
    public static string SecaoConfiguracaoDoTenant(Guid tenantId)
        => $"{SecaoConfiguracao}:{SubSecaoTenants}:{tenantId:D}";

    /// <summary>
    /// Identificador IANA do fuso do tenant (ex.: <c>America/Sao_Paulo</c>, <c>America/Manaus</c> para
    /// municipios em UTC-4). Default <see cref="FusoPadrao"/>.
    /// </summary>
    public string TimeZoneId { get; init; } = FusoPadrao;
}
