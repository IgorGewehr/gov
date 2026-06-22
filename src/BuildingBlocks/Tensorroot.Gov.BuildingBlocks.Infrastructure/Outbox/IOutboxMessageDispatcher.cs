namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Despacha UMA mensagem de Outbox já desserializada aos seus handlers (via MediatR).
/// <para>
/// Existe para ISOLAR a publicação de cada mensagem em seu PRÓPRIO escopo de DI. Sem isso, o
/// <see cref="OutboxPublisher"/> publicaria todas as mensagens do lote no MESMO escopo do contexto
/// de leitura — e, quando uma mensagem tem handlers de módulos diferentes (ex.: o domain event de
/// Finanças e o integration event consumido por Administração na mesma drenagem), o MediatR
/// resolveria DOIS <c>ModuleDbContext</c> distintos no mesmo escopo, acionando a guarda H5
/// (<see cref="ScopeDbContextHolder"/>) e descartando a contabilização.
/// </para>
/// <para>
/// A implementação que isola por escopo + tenant vive no <c>ApiHost</c> (onde há
/// <c>IServiceScopeFactory</c> e <c>TenantOverride</c>), respeitando o layering: BuildingBlocks NÃO
/// referencia ApiHost/Platform. A implementação default (<see cref="CurrentScopeOutboxMessageDispatcher"/>)
/// publica no escopo atual e serve a contextos sem isolamento (testes, hosts simples).
/// </para>
/// </summary>
public interface IOutboxMessageDispatcher
{
    /// <summary>
    /// Publica o evento aos seus handlers. As implementações de produção criam um escopo de DI NOVO,
    /// definem o tenant (<paramref name="tenantId"/>) nele e publicam ali, de modo que cada handler
    /// resolva apenas o contexto do SEU módulo.
    /// </summary>
    /// <param name="evento">Evento já desserializado a ser publicado.</param>
    /// <param name="tenantId">Tenant dono da mensagem (carimbado no escopo isolado).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task DespacharAsync(object evento, Guid tenantId, CancellationToken cancellationToken);
}
