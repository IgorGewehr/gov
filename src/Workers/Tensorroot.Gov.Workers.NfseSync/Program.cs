// Tensorroot.Gov — Worker de Sincronização NFS-e Nacional (ADN)
// Itera os tenants configurados e sincroniza as NFS-e do ADN para o banco
// DEDICADO de cada tenant (database-per-tenant). Integração PASSIVA.
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Tributos.Infrastructure;
using Tensorroot.Gov.Platform;
using Tensorroot.Gov.Workers.NfseSync;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructureBuildingBlocks();

builder.Services.AddScoped<WorkerTenantContext>();
builder.Services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<WorkerTenantContext>());
builder.Services.AddSingleton<ICurrentUser, SistemaCurrentUser>();

// Plataforma: catálogo de tenants + resolução da conexão DEDICADA por tenant.
builder.Services.AddPlatform(
    builder.Configuration["Database:Provider"] ?? "Sqlite",
    builder.Configuration.GetConnectionString("Platform") ?? "Data Source=plataforma.db");

new TributosModule().AddModule(builder.Services, builder.Configuration);

builder.Services.AddHostedService<NfseSyncWorker>();

var host = builder.Build();
host.Run();
