using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Tempo;

/// <summary>
/// Registro CENTRAL e UNICO do servico transversal de dias uteis / prazos legais (nos BuildingBlocks,
/// nao em cada modulo). Deve ser chamado UMA vez no Composition Root (ApiHost/Workers), antes dos
/// modulos. Os servicos sao <c>Scoped</c> (seguem o <c>TenantContext</c> da requisicao); o cache de
/// feriados e cross-request por chave (tenant, ano).
/// </summary>
public static class TempoServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="IFeriadosTenantProvider"/> (config + cache por tenant/ano) e
    /// <see cref="ICalendarioDiasUteis"/> (algoritmo de dias uteis). Modulos CONSOMEM por DI — nao recriam.
    /// </summary>
    /// <param name="services">Colecao de servicos.</param>
    /// <returns>A propria colecao, para encadeamento fluente.</returns>
    public static IServiceCollection AddCalendarioDiasUteis(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMemoryCache();
        services.AddScoped<IFeriadosTenantProvider, FeriadosTenantProvider>();
        services.AddScoped<ICalendarioDiasUteis, CalendarioDiasUteis>();

        // "Hoje" no FUSO do tenant (W9 — fix do WARN sistemico de fuso). Scoped: segue o TenantContext.
        // E' a UNICA porta autorizada a derivar a data civil de DOMINIO do relogio; auditoria/Outbox
        // permanecem UTC e NAO consomem esta porta.
        services.AddScoped<IDataHojeTenant, DataHojeTenant>();
        return services;
    }
}
