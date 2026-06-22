using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure;

/// <summary>Registro de DI dos blocos de construção de Infraestrutura (interceptors e tempo).</summary>
public static class InfrastructureBuildingBlocks
{
    /// <summary>Registra <see cref="TimeProvider"/> e os interceptors de auditoria e tenant.</summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>A própria coleção, para encadeamento fluente.</returns>
    public static IServiceCollection AddInfrastructureBuildingBlocks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<TenantSaveChangesInterceptor>();
        services.AddScoped<ConvertDomainEventsToOutboxInterceptor>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();

        // Verificador da cadeia de hash da trilha (A2): recomputa o encadeamento e aponta a 1ª
        // linha adulterada/removida. Sem estado — pode ser singleton, mas Scoped basta e é coerente.
        services.AddScoped<IVerificadorTrilhaAuditoria, VerificadorTrilhaAuditoria>();

        // Despachante DEFAULT (publica no escopo atual). O ApiHost SOBRESCREVE este registro por um
        // despachante que isola cada mensagem em escopo de DI próprio (com TenantOverride) — sem isso,
        // o lote publicaria todos os handlers no mesmo escopo e acionaria a guarda H5. Mantido aqui para
        // hosts simples/testes e para preservar o layering (BuildingBlocks não conhece ApiHost).
        services.AddScoped<IOutboxMessageDispatcher, CurrentScopeOutboxMessageDispatcher>();

        // Unidade de trabalho COMPARTILHADA: o DbContext de módulo ativo no escopo se registra no
        // holder ao ser construído; o ModuleUnitOfWork confirma esse contexto. Evita a colisão de
        // múltiplos módulos registrarem IUnitOfWork (em que a última registração vencia).
        services.AddScoped<ScopeDbContextHolder>();
        services.AddScoped<IUnitOfWork, ModuleUnitOfWork>();
        services.AddScoped<IIntegrationEventWriter, ModuleIntegrationEventWriter>();

        // LG-2: trilha de ACESSO a dado sensivel (leitura). Sela {Tenant,UserId,Ip,Entidade,
        // EntityId,BaseLegal,Ts} na cadeia de hash da auditoria, no DbContext de modulo ativo.
        services.AddScoped<IRegistroAcessoSensivel, RegistroAcessoSensivel>();
        return services;
    }
}
