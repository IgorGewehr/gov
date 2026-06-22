using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Infrastructure;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova do provisionamento database-per-tenant: <see cref="TributosModule.MigrarBancoAsync"/>
/// cria o schema do módulo no banco DEDICADO do tenant.
/// </summary>
public sealed class ProvisioningTests
{
    [Fact]
    public async Task MigrarBancoAsync_cria_o_schema_no_banco_dedicado_do_tenant()
    {
        var arquivo = Path.Combine(Path.GetTempPath(), $"tt_{Guid.NewGuid():N}.db");
        var conexao = "Data Source=" + arquivo;
        try
        {
            await new TributosModule().MigrarBancoAsync(
                conexao, "Sqlite", Guid.NewGuid(), EmptyServiceProvider.Instancia, CancellationToken.None);

            var tenant = new TenantContextFake(Guid.NewGuid());
            var options = new DbContextOptionsBuilder<TributosDbContext>().UseSqlite(conexao).Options;
            await using var contexto = new TributosDbContext(options, tenant);

            (await contexto.Database.CanConnectAsync()).Should().BeTrue();
            (await contexto.Contribuintes.CountAsync())
                .Should().Be(0, "a tabela do módulo deve existir no banco dedicado recém-provisionado");
        }
        finally
        {
            if (File.Exists(arquivo))
            {
                File.Delete(arquivo);
            }
        }
    }
}

file sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

/// <summary>Provedor de serviços vazio para chamadas que não consomem semeadura central.</summary>
file sealed class EmptyServiceProvider : IServiceProvider
{
    public static EmptyServiceProvider Instancia { get; } = new();

    public object? GetService(Type serviceType) => null;
}
