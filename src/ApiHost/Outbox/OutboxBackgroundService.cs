using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Platform.Persistence;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.ApiHost.Outbox;

/// <summary>
/// Serviço de segundo plano que drena periodicamente o Outbox de CADA tenant ativo, no seu
/// banco DEDICADO, publicando os eventos pendentes dos módulos licenciados (consistência eventual).
/// </summary>
internal sealed class OutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Loop resiliente: um ciclo com falha não pode derrubar o serviço.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                await DrenarTodosAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception excecao)
            {
                logger.LogError(excecao, "Falha no ciclo de drenagem do Outbox.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Falha de um módulo/tenant não pode interromper a drenagem dos demais.")]
    private async Task DrenarTodosAsync(CancellationToken cancellationToken)
    {
        List<Guid> tenants;
        await using (var escopo = scopeFactory.CreateAsyncScope())
        {
            var plataforma = escopo.ServiceProvider.GetRequiredService<PlatformDbContext>();
            tenants = await plataforma.Tenants
                .Where(tenant => tenant.Ativo)
                .Select(tenant => tenant.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var tenantId in tenants)
        {
            // Descobre os módulos licenciados num escopo de leitura próprio.
            List<string> nomesModulos;
            await using (var escopoLeitura = scopeFactory.CreateAsyncScope())
            {
                escopoLeitura.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;
                var licenciados = (await escopoLeitura.ServiceProvider.GetRequiredService<ITenantModuleProvider>()
                        .EnabledModulesAsync(tenantId, cancellationToken).ConfigureAwait(false))
                    .ToHashSet(StringComparer.Ordinal);
                nomesModulos = escopoLeitura.ServiceProvider.GetServices<IModule>()
                    .Where(modulo => licenciados.Contains(modulo.Name))
                    .Select(modulo => modulo.Name)
                    .ToList();
            }

            foreach (var nomeModulo in nomesModulos)
            {
                // Um escopo DEDICADO por módulo para a LEITURA do Outbox: o ScopeDbContextHolder mantém
                // UM único contexto de módulo por escopo. Drenar vários módulos no mesmo escopo faria o
                // holder apontar para o contexto do último módulo resolvido. Este escopo só resolve o
                // contexto de leitura do módulo (FinancasDbContext etc.) — o DESPACHO de cada mensagem aos
                // handlers ocorre em escopo PRÓPRIO (ScopedOutboxMessageDispatcher), de modo que handlers
                // de módulos distintos (ex.: contabilização em Finanças + confirmação de dotação em
                // Administração) nunca colidam no mesmo escopo (guarda H5).
                await using var escopo = scopeFactory.CreateAsyncScope();
                var serviceProvider = escopo.ServiceProvider;
                serviceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;

                var module = serviceProvider.GetServices<IModule>().FirstOrDefault(modulo => string.Equals(modulo.Name, nomeModulo, StringComparison.Ordinal));
                if (module is null)
                {
                    continue;
                }

                try
                {
                    await module.DrenarOutboxAsync(serviceProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception excecao)
                {
                    logger.LogError(excecao, "Falha ao drenar Outbox do módulo {Modulo} (tenant {Tenant}).", module.Name, tenantId);
                }
            }
        }
    }
}
