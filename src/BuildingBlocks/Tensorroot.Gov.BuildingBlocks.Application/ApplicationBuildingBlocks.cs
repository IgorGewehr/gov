using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Behaviors;

namespace Tensorroot.Gov.BuildingBlocks.Application;

/// <summary>Registro de DI dos blocos de construção de Aplicação (pipeline transversal do MediatR).</summary>
public static class ApplicationBuildingBlocks
{
    /// <summary>
    /// Registra os pipeline behaviors transversais na ordem: Logging (externo) → Validação → handler.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>A própria coleção, para encadeamento fluente.</returns>
    public static IServiceCollection AddApplicationPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
