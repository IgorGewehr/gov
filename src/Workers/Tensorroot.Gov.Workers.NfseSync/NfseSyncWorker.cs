using Tensorroot.Gov.Modules.Tributos.Application.Nfse;

namespace Tensorroot.Gov.Workers.NfseSync;

/// <summary>
/// Worker que sincroniza diariamente as NFS-e do Ambiente de Dados Nacional (ADN)
/// para cada tenant configurado, alimentando o painel fiscal (módulo Tributos).
/// Integração PASSIVA: não emitimos nem assinamos NFS-e (ADR-0003).
/// </summary>
public sealed class NfseSyncWorker(
    IServiceProvider services,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<NfseSyncWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tenants = configuration.GetSection("Nfse:Tenants").Get<List<TenantNfseConfig>>() ?? [];
        var intervaloHoras = configuration.GetValue("Nfse:IntervaloHoras", 24);
        logger.LogInformation(
            "NfseSyncWorker iniciado: {Quantidade} tenant(s) configurado(s); intervalo de {Horas}h.",
            tenants.Count,
            intervaloHoras);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SincronizarTenantsAsync(tenants, stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(TimeSpan.FromHours(intervaloHoras), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Boundary de resiliência: a falha de um tenant não pode interromper a sincronização dos demais.")]
    private async Task SincronizarTenantsAsync(IReadOnlyList<TenantNfseConfig> tenants, CancellationToken cancellationToken)
    {
        var desde = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(-1);

        foreach (var tenant in tenants)
        {
            using var scope = services.CreateScope();
            scope.ServiceProvider.GetRequiredService<WorkerTenantContext>().Definir(tenant.TenantId);
            var sincronizador = scope.ServiceProvider.GetRequiredService<INfseSincronizador>();

            try
            {
                var quantidade = await sincronizador.SincronizarAsync(tenant.Cnpjs, desde, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("NFS-e sincronizadas para o tenant {Tenant}: {Quantidade} nova(s).", tenant.TenantId, quantidade);
            }
            catch (Exception excecao)
            {
                logger.LogError(excecao, "Falha ao sincronizar NFS-e do tenant {Tenant}.", tenant.TenantId);
            }
        }
    }
}
