using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Robustez do Outbox (causa raiz da contabilização perdida): numa MESMA drenagem, mensagens cujos
/// handlers tocam MÓDULOS DISTINTOS (ex.: o domain event de Finanças que contabiliza + o integration
/// event consumido por Administração) NÃO podem ser publicadas no mesmo escopo do contexto de leitura —
/// senão dois <see cref="ModuleDbContext"/> são resolvidos no mesmo escopo e a guarda H5
/// (<see cref="ScopeDbContextHolder"/>) lança, abortando a contabilização. A correção isola o DESPACHO
/// de CADA mensagem em escopo de DI próprio, de modo que cada handler resolva só o contexto do seu módulo.
/// <para>
/// O "outro módulo" é representado por um <see cref="OutroModuloDbContext"/> local ao teste — não há
/// referência cross-módulo (que violaria o layering): basta um segundo <c>ModuleDbContext</c> distinto.
/// </para>
/// </summary>
public sealed class OutboxDispatchIsolationTests
{
    private static readonly Guid Tenant = Guid.Parse("33333333-3333-3333-3333-333333333333");

    /// <summary>
    /// Duas mensagens no lote — uma cujo handler usa Finanças, outra cujo handler usa OUTRO módulo. Com
    /// despacho ISOLADO por mensagem (escopo novo + holder próprio por mensagem), ambas são processadas
    /// sem acionar a guarda H5.
    /// </summary>
    [Fact]
    public async Task Despacho_isolado_por_mensagem_processa_handlers_de_modulos_distintos_sem_H5()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using (var ctx = NovoContextoLeitura(conexao))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        await using (var ctx = NovoContextoLeitura(conexao))
        {
            ctx.OutboxMessages.Add(Mensagem(typeof(EventoDeFinancas)));
            ctx.OutboxMessages.Add(Mensagem(typeof(EventoDeOutroModulo)));
            await ctx.SaveChangesAsync();
        }

        var raiz = ConstruirContainerIsolado(conexao);
        var dispatcher = new EscopoPorMensagemDispatcher(raiz);
        var publicador = new OutboxPublisher(dispatcher, TimeProvider.System);

        int publicadas;
        await using (var ctxLeitura = NovoContextoLeitura(conexao))
        {
            // Não deve lançar: cada mensagem é despachada em seu próprio escopo (holder próprio).
            publicadas = await publicador.PublicarPendentesAsync(ctxLeitura, lote: 50, CancellationToken.None);
        }

        publicadas.Should().Be(2);
        dispatcher.ContextosResolvidos.Should().Be(2); // um contexto por mensagem, em escopos distintos

        await using (var leitura = NovoContextoLeitura(conexao))
        {
            (await leitura.OutboxMessages.AsNoTracking().ToListAsync())
                .Should().OnlyContain(m => m.ProcessedOnUtc != null);
        }
    }

    /// <summary>
    /// Contraprova do bug: se as duas mensagens fossem despachadas no MESMO escopo (mesmo holder) — como
    /// acontecia ao publicar no escopo do contexto de leitura —, o segundo módulo no escopo LANÇA H5.
    /// É exatamente isso que o despacho isolado por mensagem elimina.
    /// </summary>
    [Fact]
    public void Sem_isolamento_dois_modulos_no_mesmo_escopo_lancam_H5()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var holder = new ScopeDbContextHolder();
        var tenant = new TenantFake();

        var financasOptions = new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(conexao).Options;
        var outroOptions = new DbContextOptionsBuilder<OutroModuloDbContext>().UseSqlite(conexao).Options;

        using var financas = new FinancasDbContext(financasOptions, tenant, holder);
        var acao = () => new OutroModuloDbContext(outroOptions, tenant, holder);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*mesmo escopo*");
    }

    private static OutboxMessage Mensagem(Type tipoEvento) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Tenant,
        Type = tipoEvento.AssemblyQualifiedName!,
        Content = "{}",
        OccurredOnUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    private static FinancasDbContext NovoContextoLeitura(SqliteConnection conexao)
    {
        var options = new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(conexao).Options;
        return new FinancasDbContext(options, new TenantFake());
    }

    private static ServiceProvider ConstruirContainerIsolado(SqliteConnection conexao)
    {
        var services = new ServiceCollection();
        services.AddSingleton(conexao);
        services.AddScoped<ITenantContext, TenantFake>();
        services.AddScoped<ScopeDbContextHolder>();
        services.AddScoped(sp => new FinancasDbContext(
            new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(sp.GetRequiredService<SqliteConnection>()).Options,
            sp.GetRequiredService<ITenantContext>(),
            sp.GetRequiredService<ScopeDbContextHolder>()));
        services.AddScoped(sp => new OutroModuloDbContext(
            new DbContextOptionsBuilder<OutroModuloDbContext>().UseSqlite(sp.GetRequiredService<SqliteConnection>()).Options,
            sp.GetRequiredService<ITenantContext>(),
            sp.GetRequiredService<ScopeDbContextHolder>()));
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Despachante de teste que espelha o do ApiHost: um escopo de DI NOVO por mensagem. O "handler" de
    /// cada mensagem resolve APENAS o contexto do seu módulo — nunca dois no mesmo escopo, então a guarda
    /// H5 nunca é acionada.
    /// </summary>
    private sealed class EscopoPorMensagemDispatcher(IServiceProvider raiz) : IOutboxMessageDispatcher
    {
        public int ContextosResolvidos { get; private set; }

        public Task DespacharAsync(object evento, Guid tenantId, CancellationToken cancellationToken)
        {
            using var escopo = raiz.CreateScope();
            if (evento is EventoDeFinancas)
            {
                _ = escopo.ServiceProvider.GetRequiredService<FinancasDbContext>();
            }
            else
            {
                _ = escopo.ServiceProvider.GetRequiredService<OutroModuloDbContext>();
            }

            ContextosResolvidos++;
            return Task.CompletedTask;
        }
    }

    private sealed record EventoDeFinancas : IDomainEvent;

    private sealed record EventoDeOutroModulo : IDomainEvent;

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Tenant;

        public bool HasTenant => true;
    }

    /// <summary>Segundo <see cref="ModuleDbContext"/> (proxy de "outro módulo"), local ao teste.</summary>
    private sealed class OutroModuloDbContext(
        DbContextOptions<OutroModuloDbContext> options,
        ITenantContext tenantContext,
        ScopeDbContextHolder holder)
        : ModuleDbContext(options, tenantContext, holder)
    {
        public override string Schema => "outro_modulo";
    }
}
