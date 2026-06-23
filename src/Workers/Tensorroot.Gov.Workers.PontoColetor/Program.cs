// Tensorroot.Gov — Worker Coletor de Ponto (hardware REP -> AFD -> dominio)
// Itera os tenants configurados e, para cada REP ativo, coleta o AFD do equipamento e o INGERE de
// forma idempotente no banco DEDICADO do tenant (database-per-tenant). Espelha o NfseSync.
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;
using Tensorroot.Gov.Platform;
using Tensorroot.Gov.Workers.PontoColetor;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructureBuildingBlocks();

builder.Services.AddScoped<WorkerTenantContext>();
builder.Services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<WorkerTenantContext>());
builder.Services.AddSingleton<ICurrentUser, SistemaCurrentUser>();

// Plataforma: catalogo de tenants + resolucao da conexao DEDICADA por tenant.
builder.Services.AddPlatform(
    builder.Configuration["Database:Provider"] ?? "Sqlite",
    builder.Configuration.GetConnectionString("Platform") ?? "Data Source=plataforma.db");

// Registra DbContext, repositorios, parser do AFD, drivers de coleta (ACL) e os handlers (MediatR).
new RecursosHumanosModule().AddModule(builder.Services, builder.Configuration);

builder.Services.AddHostedService<PontoColetorWorker>();

var host = builder.Build();
host.Run();
