using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Empenhos;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.Tributos.Contracts;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Prova de comunicação inter-módulos: um Integration Event publicado pelo Tributos
/// (via Contracts) é consumido por um handler do MediatR em Finanças e persistido —
/// sem que Finanças dependa de qualquer interno de Tributos (guardado pelas fitness functions).
/// </summary>
public sealed class IntegracaoCrossModuleTests
{
    [Fact]
    public async Task Evento_de_integracao_de_Tributos_e_consumido_e_persistido_por_Financas()
    {
        var tenantId = Guid.NewGuid();
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddSingleton<ITenantContext>(new TenantContextFake(tenantId));
        services.AddSingleton<ICurrentUser>(new CurrentUserFake());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TenantSaveChangesInterceptor>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<FinancasDbContext>((sp, options) => options
            .UseSqlite(connection)
            .AddInterceptors(
                sp.GetRequiredService<TenantSaveChangesInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FinancasDbContext>());
        services.AddScoped<IReceitaArrecadadaRepository, ReceitaArrecadadaRepository>();
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(EmpenharCommand).Assembly));

        await using var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<FinancasDbContext>().Database.EnsureCreatedAsync();
        }

        // Publica o evento de integração de Tributos através do MediatR.
        using (var scope = provider.CreateScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
            var evento = new ReceitaArrecadadaIntegrationEvent(
                Guid.NewGuid(),
                TimeProvider.System.GetUtcNow().UtcDateTime,
                tenantId,
                Guid.NewGuid(),
                1500.00m,
                new DateOnly(2026, 6, 20));
            await publisher.Publish(evento);
        }

        // Finanças deve ter registrado a receita.
        using (var scope = provider.CreateScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<FinancasDbContext>();
            var receitas = await contexto.ReceitasArrecadadas.ToListAsync();
            receitas.Should().ContainSingle();
            receitas[0].Valor.Valor.Should().Be(1500.00m);
            receitas[0].TenantId.Should().Be(tenantId);
        }
    }
}

file sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserFake : ICurrentUser
{
    public string? UserId => "teste";

    public string? UserName => "Usuário de Teste";

    public string? IpAddress => "127.0.0.1";
}
