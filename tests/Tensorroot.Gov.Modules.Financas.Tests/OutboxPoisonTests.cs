using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Achado H1: o Outbox deve ser resiliente a poison message. Uma mensagem que SEMPRE falha não pode
/// reprocessar para sempre nem bloquear a cabeça do lote das mensagens válidas. Após N tentativas
/// (<see cref="OutboxMessage.MaxAttempts"/>) ela vira dead-letter (não reprocessa) com o erro registrado.
/// </summary>
public sealed class OutboxPoisonTests
{
    private static readonly Guid Tenant = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private static FinancasDbContext NovoContexto(SqliteConnection conexao)
    {
        var options = new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(conexao).Options;
        return new FinancasDbContext(options, new TenantFake());
    }

    [Fact]
    public async Task Poison_vai_para_dead_letter_apos_N_tentativas_e_nao_bloqueia_as_validas()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using (var ctx = NovoContexto(conexao))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        // Uma mensagem de tipo INEXISTENTE (Type.GetType retorna null) é veneno permanente.
        var poisonId = Guid.NewGuid();
        var validaId = Guid.NewGuid();
        await using (var ctx = NovoContexto(conexao))
        {
            ctx.OutboxMessages.Add(new OutboxMessage
            {
                Id = poisonId,
                TenantId = Tenant,
                Type = "Tipo.Que.Nao.Existe, Assembly.Inexistente",
                Content = "{}",
                OccurredOnUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), // mais ANTIGA: fica na cabeça do lote
            });
            ctx.OutboxMessages.Add(new OutboxMessage
            {
                Id = validaId,
                TenantId = Tenant,
                Type = typeof(EventoValido).AssemblyQualifiedName!,
                Content = "{}",
                OccurredOnUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
            await ctx.SaveChangesAsync();
        }

        var tempo = new TempoFake(new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero));
        var publisher = new PublisherFake();
        var publicador = new OutboxPublisher(new CurrentScopeOutboxMessageDispatcher(publisher), tempo);

        // Drena várias vezes, avançando o relógio além do backoff a cada ciclo para reabilitar a poison.
        for (var ciclo = 0; ciclo < OutboxMessage.MaxAttempts + 2; ciclo++)
        {
            await using var ctx = NovoContexto(conexao);
            await publicador.PublicarPendentesAsync(ctx, lote: 100, CancellationToken.None);
            tempo.Avancar(TimeSpan.FromHours(1)); // bem além do teto de backoff (~15min)
        }

        await using (var leitura = NovoContexto(conexao))
        {
            var poison = await leitura.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == poisonId);
            var valida = await leitura.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == validaId);

            // A poison: dead-letter após exatamente MaxAttempts, com erro registrado e SEM processamento.
            poison.AttemptCount.Should().Be(OutboxMessage.MaxAttempts);
            poison.DeadLetteredOnUtc.Should().NotBeNull();
            poison.ProcessedOnUtc.Should().BeNull();
            poison.Error.Should().NotBeNullOrEmpty();

            // A válida NÃO ficou bloqueada pela poison na cabeça do lote: foi processada.
            valida.ProcessedOnUtc.Should().NotBeNull();
            valida.DeadLetteredOnUtc.Should().BeNull();
        }

        // A válida foi publicada exatamente uma vez (idempotência do drain).
        publisher.Publicados.Should().Be(1);
    }

    [Fact]
    public async Task Mensagem_recem_falhada_fica_adiada_pelo_backoff_e_nao_e_reprocessada_no_mesmo_ciclo()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using (var ctx = NovoContexto(conexao))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        var poisonId = Guid.NewGuid();
        await using (var ctx = NovoContexto(conexao))
        {
            ctx.OutboxMessages.Add(new OutboxMessage
            {
                Id = poisonId,
                TenantId = Tenant,
                Type = "Tipo.Inexistente, Nada",
                Content = "{}",
                OccurredOnUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
            await ctx.SaveChangesAsync();
        }

        var tempo = new TempoFake(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));
        var publicador = new OutboxPublisher(new CurrentScopeOutboxMessageDispatcher(new PublisherFake()), tempo);

        // Primeiro ciclo: falha uma vez e agenda backoff no futuro.
        await using (var ctx = NovoContexto(conexao))
        {
            await publicador.PublicarPendentesAsync(ctx, lote: 100, CancellationToken.None);
        }

        // Segundo ciclo SEM avançar o relógio: NextAttemptUtc ainda no futuro → não é selecionada.
        await using (var ctx = NovoContexto(conexao))
        {
            await publicador.PublicarPendentesAsync(ctx, lote: 100, CancellationToken.None);
        }

        await using var leitura = NovoContexto(conexao);
        var poison = await leitura.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == poisonId);
        poison.AttemptCount.Should().Be(1); // só a 1ª tentativa contou; a 2ª foi adiada pelo backoff
        poison.NextAttemptUtc.Should().NotBeNull();
        poison.NextAttemptUtc.Should().BeAfter(tempo.GetUtcNow().UtcDateTime);
    }

    /// <summary>Evento de domínio mínimo (sempre desserializa OK) usado como mensagem válida.</summary>
    public sealed record EventoValido : IDomainEvent;

    private sealed class PublisherFake : IPublisher
    {
        public int Publicados { get; private set; }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Publicados++;
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Publicados++;
            return Task.CompletedTask;
        }
    }

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Tenant;

        public bool HasTenant => true;
    }

    private sealed class TempoFake(DateTimeOffset inicio) : TimeProvider
    {
        private DateTimeOffset _agora = inicio;

        public override DateTimeOffset GetUtcNow() => _agora;

        public void Avancar(TimeSpan delta) => _agora += delta;
    }
}
