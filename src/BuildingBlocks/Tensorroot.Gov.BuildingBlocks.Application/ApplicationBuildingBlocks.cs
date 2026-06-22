using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Behaviors;

namespace Tensorroot.Gov.BuildingBlocks.Application;

/// <summary>Registro de DI dos blocos de construção de Aplicação (pipeline transversal do MediatR).</summary>
public static class ApplicationBuildingBlocks
{
    /// <summary>
    /// Registra os pipeline behaviors transversais na ordem: Logging (externo) → Validação →
    /// Trilha de acesso LGPD (LG-2, mais interno: sela o acesso logo apos o handler de uma query
    /// <see cref="Messaging.ISensivelLgpd"/>) → handler.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>A própria coleção, para encadeamento fluente.</returns>
    public static IServiceCollection AddApplicationPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TrilhaAcessoSensivelBehavior<,>));
        return services;
    }
}
