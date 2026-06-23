using MediatR;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;

namespace Tensorroot.Gov.Workers.PontoColetor;

/// <summary>
/// Worker que COLETA periodicamente o AFD dos equipamentos REP cadastrados em cada tenant e o INGERE
/// no dominio de ponto (Portaria MTP 671/2021), de forma idempotente. Espelha a topologia do
/// <c>NfseSync</c>: BackgroundService em loop por intervalo, multi-tenant por iteracao explicita, atras
/// de ACL (drivers por fabricante) e com Polly nos drivers de rede. Online-first + reconciliacao: o
/// dedup por (REP, NSR) torna o push em tempo real (endpoint da ApiHost) e a varredura do worker
/// convergentes e seguros.
/// </summary>
public sealed class PontoColetorWorker(
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<PontoColetorWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tenants = configuration.GetSection("Ponto:Tenants").Get<List<TenantPontoConfig>>() ?? [];
        var intervaloMinutos = configuration.GetValue("Ponto:IntervaloMinutos", 60);
        logger.LogInformation(
            "PontoColetorWorker iniciado: {Quantidade} tenant(s) configurado(s); intervalo de {Minutos} min.",
            tenants.Count,
            intervaloMinutos);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ColetarTenantsAsync(tenants, stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervaloMinutos), stoppingToken).ConfigureAwait(false);
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
        Justification = "Boundary de resiliencia: a falha de um REP/tenant nao pode interromper a coleta dos demais.")]
    private async Task ColetarTenantsAsync(IReadOnlyList<TenantPontoConfig> tenants, CancellationToken cancellationToken)
    {
        foreach (var tenant in tenants)
        {
            using var scope = services.CreateScope();
            scope.ServiceProvider.GetRequiredService<WorkerTenantContext>().Definir(tenant.TenantId);
            var reps = scope.ServiceProvider.GetRequiredService<IRepRepository>();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            IReadOnlyList<Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto.RepConfigurado> ativos;
            try
            {
                ativos = await reps.ListarAtivosAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception excecao)
            {
                logger.LogError(excecao, "Falha ao listar REPs ativos do tenant {Tenant}.", tenant.TenantId);
                continue;
            }

            foreach (var rep in ativos)
            {
                try
                {
                    var resultado = await sender
                        .Send(new ColetarRepCommand(rep.Id.Value), cancellationToken)
                        .ConfigureAwait(false);
                    logger.LogInformation(
                        "Coleta do REP {Rep} (tenant {Tenant}): {Novas} nova(s), {Duplicadas} duplicada(s), {Pendentes} pendente(s), integridade={Integridade}.",
                        rep.IdentificacaoEquipamento,
                        tenant.TenantId,
                        resultado.Novas,
                        resultado.Duplicadas,
                        resultado.PendentesDeVinculo,
                        resultado.IntegridadeOk);
                }
                catch (Exception excecao)
                {
                    logger.LogError(excecao, "Falha ao coletar o REP {Rep} do tenant {Tenant}.", rep.IdentificacaoEquipamento, tenant.TenantId);
                }
            }
        }
    }
}
