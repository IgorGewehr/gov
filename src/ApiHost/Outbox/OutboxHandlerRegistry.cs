using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Tensorroot.Gov.ApiHost.Outbox;

/// <summary>
/// Mapa, capturado na composição (root), de cada tipo de evento → os tipos de IMPLEMENTAÇÃO dos seus
/// <see cref="INotificationHandler{TNotification}"/> registrados (de quaisquer módulos). Permite ao
/// <see cref="ScopedOutboxMessageDispatcher"/> resolver e invocar CADA consumidor em um escopo de DI
/// PRÓPRIO — sem enumerar o <c>IEnumerable&lt;INotificationHandler&gt;</c> num único escopo (o que
/// construiria todos os handlers e, com eles, dois <c>ModuleDbContext</c> distintos no mesmo escopo,
/// acionando a guarda H5 ao drenar um evento consumido por módulos diferentes — achado de runtime M8).
/// </summary>
public sealed class OutboxHandlerRegistry
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>> _handlersPorNotificacao;

    private OutboxHandlerRegistry(IReadOnlyDictionary<Type, IReadOnlyList<Type>> handlersPorNotificacao)
        => _handlersPorNotificacao = handlersPorNotificacao;

    /// <summary>
    /// Constrói o registro a partir das registrações de <see cref="INotificationHandler{TNotification}"/>
    /// presentes na coleção de serviços (todas já adicionadas pelos módulos via <c>AddMediatR</c>).
    /// </summary>
    /// <param name="services">Coleção de serviços da composição.</param>
    /// <returns>Registro imutável evento → handlers.</returns>
    public static OutboxHandlerRegistry Construir(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var mapa = new Dictionary<Type, List<Type>>();
        foreach (var descriptor in services)
        {
            var servico = descriptor.ServiceType;
            if (!servico.IsGenericType || servico.GetGenericTypeDefinition() != typeof(INotificationHandler<>))
            {
                continue;
            }

            var implementacao = descriptor.ImplementationType;
            if (implementacao is null)
            {
                continue;
            }

            var notificacao = servico.GetGenericArguments()[0];
            if (!mapa.TryGetValue(notificacao, out var lista))
            {
                lista = [];
                mapa[notificacao] = lista;
            }

            if (!lista.Contains(implementacao))
            {
                lista.Add(implementacao);
            }
        }

        var imutavel = mapa.ToDictionary(par => par.Key, par => (IReadOnlyList<Type>)par.Value);
        return new OutboxHandlerRegistry(imutavel);
    }

    /// <summary>Tipos de implementação dos handlers registrados para um tipo de evento (ou vazio).</summary>
    /// <param name="tipoEvento">Tipo concreto do evento.</param>
    /// <returns>Tipos de implementação dos consumidores.</returns>
    public IReadOnlyList<Type> HandlersDe(Type tipoEvento)
        => _handlersPorNotificacao.TryGetValue(tipoEvento, out var handlers) ? handlers : [];
}
