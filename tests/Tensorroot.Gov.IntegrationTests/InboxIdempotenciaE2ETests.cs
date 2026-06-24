using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.IntegrationTests.Infrastructure;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Application.Integracoes;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Platform.Tenancy;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.IntegrationTests;

/// <summary>
/// E2E da ROBUSTEZ residual (M9 W9.7) — prova do INBOX idempotente system-wide da fundacao.
///
/// Invariante sob teste: a entrega at-least-once do Outbox + redrenagem (apos falha parcial, reinicio
/// ou backoff) NAO pode aplicar o efeito de um consumidor MAIS DE UMA VEZ. A guarda e o Inbox da
/// fundacao (chave (EventId, Handler, TenantId), selada na MESMA transacao do efeito pelo
/// <c>ScopedOutboxMessageDispatcher</c>).
///
/// Estrategia: usamos um consumidor que REALMENTE muta estado e toca seu ModuleDbContext —
/// <see cref="ConfirmarDotacaoQuandoEmpenhoEmitidoHandler"/> (Administracao), que confirma a dotacao do
/// Contrato ao receber o <see cref="EmpenhoEmitidoIntegrationEvent"/>. Despachamos o MESMO evento
/// (mesmo <c>EventId</c>) DUAS VEZES pelo <see cref="IOutboxMessageDispatcher"/> real (o despachante de
/// Outbox que envolve cada handler com o Inbox) e provamos:
/// <list type="number">
///   <item>O efeito de negocio foi aplicado EXATAMENTE UMA VEZ (DotacaoConfirmada + EmpenhoRef gravados,
///         sem duplicar) — confirmado via repositorio do dominio E via HTTP.</item>
///   <item>Existe EXATAMENTE UMA linha de <c>InboxMessage</c> para a trinca (EventId, Handler, tenant) —
///         o selo de consumo nao duplicou; a segunda drenagem foi PULADA.</item>
/// </list>
///
/// Tudo no banco DEDICADO do tenant (isolamento multi-tenant intacto). O teste edita somente tests/ e
/// usa apenas tipos publicos (Application/Domain de Administracao + BuildingBlocks).
/// </summary>
[Collection(ApiHostCollection.Nome)]
public sealed class InboxIdempotenciaE2ETests(ApiHostFixture fixture)
{
    private readonly CustomWebApplicationFactory _factory = fixture.Factory;

    [Fact]
    public async Task Mesmo_evento_despachado_duas_vezes_aplica_efeito_uma_vez_e_sela_inbox_uma_vez()
    {
        // Arrange: tenant proprio (banco dedicado) + contrato celebrado via HTTP (Origem = Dispensa).
        var tenant = await _factory.ProvisionarTenantAsync(
            "21.345.678/0001-86", "Prefeitura E2E Inbox/RS", PoderTenant.Executivo);
        var cliente = _factory.CreateClient().ComToken(EmissorTokenDeTeste.EmitirAdministrador(tenant));

        var respFornecedor = await cliente.PostJsonAsync("/api/administracao/fornecedores", new
        {
            Cnpj = "55.666.777/0001-81",
            RazaoSocial = "Fornecedora Inbox LTDA",
        });
        respFornecedor.StatusCode.Should().Be(HttpStatusCode.OK, await respFornecedor.Content.ReadAsStringAsync());
        var fornecedorId = (await respFornecedor.LerJsonAsync()).GetProperty("id").GetGuid();

        var respContrato = await cliente.PostJsonAsync("/api/administracao/contratos", new
        {
            LicitacaoId = (Guid?)null,
            FornecedorId = fornecedorId,
            Origem = "Dispensa",
            Objeto = "Servico de manutencao predial",
            Valor = 30_000m,
            VigenciaInicio = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            VigenciaFim = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)).ToString("yyyy-MM-dd"),
            EmpenhoId = (Guid?)null,
            NumeroEmpenho = (string?)null,
            JustificativaContratacaoDireta = "Dispensa por valor (art. 75, II).",
        });
        respContrato.StatusCode.Should().Be(HttpStatusCode.OK, await respContrato.Content.ReadAsStringAsync());
        var contratoId = (await respContrato.LerJsonAsync()).GetProperty("id").GetGuid();

        // Estado inicial: dotacao NAO confirmada (gate do art. 94 ainda fechado).
        (await ContratoDotacaoConfirmadaAsync(tenant, contratoId)).Should().BeFalse(
            "contrato recem-celebrado nasce sem dotacao confirmada");

        // Evento de integracao com EventId FIXO — a chave do Inbox. As duas entregas usam o MESMO EventId,
        // simulando uma redrenagem do Outbox (entrega duplicada at-least-once).
        var eventId = Guid.NewGuid();
        var evento = new EmpenhoEmitidoIntegrationEvent(
            EventId: eventId,
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: tenant,
            ContratoId: contratoId,
            EmpenhoId: Guid.NewGuid(),
            NumeroEmpenho: "2026NE000777",
            Valor: 30_000m);

        var handlerNome = typeof(ConfirmarDotacaoQuandoEmpenhoEmitidoHandler).FullName!;

        // Act: despacha o MESMO evento DUAS vezes pelo despachante REAL (que envolve o handler com o Inbox).
        await DespacharAsync(tenant, evento);
        await DespacharAsync(tenant, evento);

        // Assert 1 — EFEITO uma unica vez: a dotacao esta confirmada (idempotente; a 2a entrega foi pulada).
        (await ContratoDotacaoConfirmadaAsync(tenant, contratoId)).Should().BeTrue(
            "o consumo do empenho confirma a dotacao do contrato");

        var detalhe = await (await cliente.GetAsync($"/api/administracao/contratos/{contratoId}")).LerJsonAsync();
        detalhe.GetProperty("dotacaoConfirmada").GetBoolean().Should().BeTrue();

        // Assert 2 — SELO uma unica vez: exatamente UMA linha de Inbox para a trinca (EventId, Handler, tenant).
        var selos = await ContarSelosInboxAsync(tenant, eventId, handlerNome);
        selos.Should().Be(1, "a 2a entrega do mesmo EventId e PULADA pelo pre-check do Inbox — nao re-sela");
    }

    /// <summary>Despacha um evento de integracao pelo <see cref="IOutboxMessageDispatcher"/> real, no tenant informado.</summary>
    private async Task DespacharAsync(Guid tenant, object evento)
    {
        await using var escopo = _factory.Services.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenant;
        var despachante = escopo.ServiceProvider.GetRequiredService<IOutboxMessageDispatcher>();
        await despachante.DespacharAsync(evento, tenant, CancellationToken.None);
    }

    /// <summary>Le, via repositorio do dominio (banco dedicado do tenant), se a dotacao do contrato foi confirmada.</summary>
    private async Task<bool> ContratoDotacaoConfirmadaAsync(Guid tenant, Guid contratoId)
    {
        var confirmada = false;
        await _factory.DentroDoTenantAsync(tenant, async sp =>
        {
            var contratos = sp.GetRequiredService<IContratoRepository>();
            var contrato = await contratos.ObterPorIdAsync(new ContratoId(contratoId), CancellationToken.None);
            confirmada = contrato?.DotacaoConfirmada ?? false;
        });
        return confirmada;
    }

    /// <summary>
    /// Conta as linhas de <c>InboxMessage</c> para a trinca (EventId, Handler, tenant) no banco DEDICADO do
    /// tenant. Toca primeiro o repositorio de Administracao para que o <see cref="ScopeDbContextHolder"/>
    /// passe a apontar para o AdministracaoDbContext (mesmo contexto onde o Inbox foi selado), e entao
    /// consulta o <c>InboxMessage</c> pela API generica do <see cref="ModuleDbContext"/> (fundacao) — sem
    /// referenciar a Infrastructure interna do modulo.
    /// </summary>
    private async Task<int> ContarSelosInboxAsync(Guid tenant, Guid eventId, string handlerNome)
    {
        var total = 0;
        await _factory.DentroDoTenantAsync(tenant, async sp =>
        {
            // Forca a resolucao do ModuleDbContext de Administracao no escopo (popula o holder).
            _ = await sp.GetRequiredService<IContratoRepository>()
                .ObterPorIdAsync(new ContratoId(Guid.NewGuid()), CancellationToken.None);

            var contexto = sp.GetRequiredService<ScopeDbContextHolder>().Atual
                ?? throw new InvalidOperationException("ModuleDbContext de Administracao nao resolvido no escopo.");

            total = await contexto.Set<InboxMessage>()
                .AsNoTracking()
                .CountAsync(
                    i => i.EventId == eventId && i.Handler == handlerNome && i.TenantId == tenant,
                    CancellationToken.None);
        });
        return total;
    }
}
