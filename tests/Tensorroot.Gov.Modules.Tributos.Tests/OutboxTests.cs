using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova do Outbox Pattern: os eventos de domínio acumulados nas entidades são
/// materializados como <c>OutboxMessage</c> na MESMA transação do SaveChanges,
/// e a entidade tem seus eventos limpos.
/// </summary>
public sealed class OutboxTests
{
    [Fact]
    public async Task Eventos_de_dominio_viram_OutboxMessage_e_a_entidade_e_limpa()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var tenantId = Guid.NewGuid();
        var tenant = new TenantContextFake(tenantId);

        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(conexao)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenant),
                new ConvertDomainEventsToOutboxInterceptor(TimeProvider.System))
            .Options;

        await using var contexto = new TributosDbContext(options, tenant);
        await contexto.Database.EnsureCreatedAsync();

        var contribuinte = Contribuinte.PessoaFisica(tenantId, Cpf.Create("529.982.247-25"), "Maria Teste");
        contexto.Contribuintes.Add(contribuinte);
        await contexto.SaveChangesAsync();

        var outbox = await contexto.OutboxMessages.ToListAsync();
        outbox.Should().ContainSingle("o evento de domínio deve virar uma mensagem de Outbox");
        outbox[0].Type.Should().Contain("ContribuinteCadastrado");
        outbox[0].TenantId.Should().Be(tenantId);
        contribuinte.DomainEvents.Should().BeEmpty("os eventos devem ser limpos após materializados");
    }
}

file sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}
