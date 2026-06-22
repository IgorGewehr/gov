using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova do lado de PUBLICAÇÃO do Outbox: as mensagens persistidas são publicadas
/// (via MediatR) e marcadas como processadas.
/// </summary>
public sealed class OutboxPublisherTests
{
    [Fact]
    public async Task Publica_mensagens_pendentes_e_marca_como_processadas()
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

        // Gera um evento de domínio → vira OutboxMessage (via interceptor).
        contexto.Contribuintes.Add(Contribuinte.PessoaFisica(tenantId, Cpf.Create("529.982.247-25"), "Maria"));
        await contexto.SaveChangesAsync();

        var fakePublisher = new FakePublisher();
        var publicador = new OutboxPublisher(new CurrentScopeOutboxMessageDispatcher(fakePublisher), TimeProvider.System);

        var publicadas = await publicador.PublicarPendentesAsync(contexto, lote: 50, CancellationToken.None);

        publicadas.Should().Be(1);
        fakePublisher.Publicados.Should().ContainSingle().Which.Should().BeOfType<ContribuinteCadastrado>();
        (await contexto.OutboxMessages.ToListAsync()).Should().OnlyContain(mensagem => mensagem.ProcessedOnUtc != null);
    }
}

file sealed class FakePublisher : IPublisher
{
    public List<object> Publicados { get; } = [];

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        Publicados.Add(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        Publicados.Add(notification!);
        return Task.CompletedTask;
    }
}

file sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}
