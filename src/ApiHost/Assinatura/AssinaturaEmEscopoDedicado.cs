using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;

namespace Tensorroot.Gov.ApiHost.Assinatura;

/// <summary>
/// Implementacao de <see cref="IAssinaturaEmEscopoDedicado"/>: assina com o A1 do tenant num ESCOPO de
/// DI proprio, evitando o gatilho da guarda H5 (<c>ScopeDbContextHolder</c>) quando um handler de outro
/// modulo (ex.: AFD/AEJ do RecursosHumanos) ja resolveu o seu <c>ModuleDbContext</c> no escopo da
/// requisicao.
/// <para>
/// Para cada chamada abre um escopo novo, reaplica o <see cref="TenantOverride"/> com o tenant corrente
/// (preserva o banco dedicado e os Global Query Filters) e resolve o <see cref="IServicoAssinaturaDigital"/>
/// ali — onde o UNICO <c>ModuleDbContext</c> e o do Cofre. Mesmo padrao do <c>ScopedOutboxMessageDispatcher</c>.
/// Vive no ApiHost porque depende de <see cref="IServiceScopeFactory"/> + <see cref="TenantOverride"/>
/// (camada superior ao BuildingBlocks), preservando o layering.
/// </para>
/// </summary>
internal sealed class AssinaturaEmEscopoDedicado(
    IServiceScopeFactory scopeFactory,
    ITenantContext tenantContext) : IAssinaturaEmEscopoDedicado
{
    /// <inheritdoc />
    public async Task<byte[]> AssinarCmsAsync(
        ReadOnlyMemory<byte> conteudo,
        OpcoesAssinaturaCms opcoes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        // Captura o tenant ANTES de abrir o novo escopo (o ITenantContext do novo escopo nao tem HTTP).
        var tenantId = tenantContext.TenantId;

        await using var escopo = scopeFactory.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;

        var assinatura = escopo.ServiceProvider.GetRequiredService<IServicoAssinaturaDigital>();
        return await assinatura.AssinarCmsAsync(conteudo, opcoes, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ResultadoAssinatura> AssinarXmlAsync(
        ReadOnlyMemory<byte> xmlUtf8,
        OpcoesAssinaturaXml opcoes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        var tenantId = tenantContext.TenantId;

        await using var escopo = scopeFactory.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;

        var assinatura = escopo.ServiceProvider.GetRequiredService<IServicoAssinaturaDigital>();
        return await assinatura.AssinarXmlAsync(xmlUtf8, opcoes, cancellationToken).ConfigureAwait(false);
    }
}
