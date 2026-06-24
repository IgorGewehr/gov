using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Tempo;

/// <summary>
/// Implementacao de <see cref="IDataHojeTenant"/>: deriva a data/instante civil do tenant a partir do
/// instante UTC do <see cref="TimeProvider"/> convertido para o FUSO do tenant (config por tenant em
/// <c>Tempo:Fuso:Tenants:{tenantId}</c>, fallback secao raiz <c>Tempo:Fuso</c>, default
/// <see cref="OpcoesFusoTenant.FusoPadrao"/>). O tenant e' resolvido via <see cref="ITenantContext"/>
/// (mesma mecanica do <c>FeriadosTenantProvider</c>). O instante segue do <c>TimeProvider</c> (testavel
/// com <c>FakeTimeProvider</c>); apenas a CONVERSAO para data civil aplica o fuso. Auditoria/Outbox
/// continuam UTC e NAO usam esta classe.
/// </summary>
public sealed class DataHojeTenant(
    TimeProvider timeProvider,
    IConfiguration configuration,
    ITenantContext tenant)
    : IDataHojeTenant
{
    // TimeZoneInfo e' imutavel/thread-safe e a resolucao por Id e' relativamente cara: cacheia por Id.
    private static readonly ConcurrentDictionary<string, TimeZoneInfo> FusosResolvidos = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public DateOnly Hoje() => DateOnly.FromDateTime(Agora().DateTime);

    /// <inheritdoc />
    public DateTimeOffset Agora()
        => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), ResolverFusoDoTenant());

    private TimeZoneInfo ResolverFusoDoTenant()
    {
        var timeZoneId = ResolverTimeZoneIdDoTenant();

        return FusosResolvidos.GetOrAdd(timeZoneId, id =>
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // Configuracao invalida nao pode silenciar para UTC (erraria a data civil de novo).
                // O default de fabrica (America/Sao_Paulo) e' garantido na plataforma; um Id custom
                // invalido e' erro de configuracao do tenant — falha alto.
                return TimeZoneInfo.FindSystemTimeZoneById(OpcoesFusoTenant.FusoPadrao);
            }
        });
    }

    /// <summary>
    /// Resolve o <c>TimeZoneId</c> do PROPRIO tenant: le <c>Tempo:Fuso:Tenants:{tenantId}</c>; na
    /// ausencia (ente unico / piloto single-tenant), cai para a secao raiz <c>Tempo:Fuso</c>; na
    /// ausencia de ambas, usa o default <see cref="OpcoesFusoTenant.FusoPadrao"/>.
    /// </summary>
    private string ResolverTimeZoneIdDoTenant()
    {
        var secaoTenant = configuration
            .GetSection(OpcoesFusoTenant.SecaoConfiguracaoDoTenant(tenant.TenantId));

        var secao = secaoTenant.Exists()
            ? secaoTenant
            : configuration.GetSection(OpcoesFusoTenant.SecaoConfiguracao);

        var opcoes = secao.Get<OpcoesFusoTenant>() ?? new OpcoesFusoTenant();

        return string.IsNullOrWhiteSpace(opcoes.TimeZoneId)
            ? OpcoesFusoTenant.FusoPadrao
            : opcoes.TimeZoneId;
    }
}
