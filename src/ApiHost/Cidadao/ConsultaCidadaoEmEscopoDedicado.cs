using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;

namespace Tensorroot.Gov.ApiHost.Cidadao;

/// <summary>
/// Implementacao de <see cref="IConsultaCidadaoEmEscopoDedicado"/>: executa a consulta de leitura cidada
/// de um modulo-fonte (Tributos/Protocolo) num ESCOPO DE DI proprio, evitando o gatilho da guarda H5
/// (<c>ScopeDbContextHolder</c>) — no Portal do Cidadao, o handler ja resolveu o <c>CidadaoDbContext</c>
/// no escopo da requisicao (ancora dado-proprio + trilha LGPD), entao a consulta do modulo-fonte
/// resolveria um SEGUNDO <c>ModuleDbContext</c> lado-a-lado.
/// <para>
/// Para cada chamada abre um escopo novo, reaplica o <see cref="TenantOverride"/> com o tenant corrente
/// (preserva o banco dedicado e os Global Query Filters) e resolve a porta de consulta ali — onde o UNICO
/// <c>ModuleDbContext</c> e o do modulo-fonte. Mesmo padrao do <c>AssinaturaEmEscopoDedicado</c> e do
/// <c>ScopedOutboxMessageDispatcher</c>. Vive no ApiHost porque depende de <see cref="IServiceScopeFactory"/>
/// + <see cref="TenantOverride"/> (camada superior ao BuildingBlocks), preservando o layering.
/// </para>
/// </summary>
internal sealed class ConsultaCidadaoEmEscopoDedicado(
    IServiceScopeFactory scopeFactory,
    ITenantContext tenantContext) : IConsultaCidadaoEmEscopoDedicado
{
    /// <inheritdoc />
    public async Task<TResultado> ExecutarAsync<TConsulta, TResultado>(
        Func<TConsulta, CancellationToken, Task<TResultado>> consulta,
        CancellationToken cancellationToken)
        where TConsulta : notnull
    {
        ArgumentNullException.ThrowIfNull(consulta);

        // Captura o tenant ANTES de abrir o novo escopo (o ITenantContext do novo escopo nao tem HTTP).
        var tenantId = tenantContext.TenantId;

        await using var escopo = scopeFactory.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;

        var porta = escopo.ServiceProvider.GetRequiredService<TConsulta>();
        return await consulta(porta, cancellationToken).ConfigureAwait(false);
    }
}
