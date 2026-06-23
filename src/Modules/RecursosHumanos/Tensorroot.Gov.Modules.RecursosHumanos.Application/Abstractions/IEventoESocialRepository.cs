using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="EventoESocial"/> (respeita o Global Query Filter por tenant).</summary>
public interface IEventoESocialRepository
{
    /// <summary>Marca um novo evento eSocial para insercao.</summary>
    /// <param name="evento">Evento a adicionar.</param>
    void Adicionar(EventoESocial evento);

    /// <summary>Obtem um evento por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O evento, ou <c>null</c> se inexistente no tenant.</returns>
    Task<EventoESocial?> ObterPorIdAsync(EventoESocialId id, CancellationToken cancellationToken);

    /// <summary>Obtem o evento de uma chave de idempotencia (geracao idempotente — ESOCIAL-SPEC §4.3).</summary>
    /// <param name="chave">Chave de negocio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O evento existente, ou <c>null</c>.</returns>
    Task<EventoESocial?> ObterPorChaveAsync(ChaveIdempotenciaEvento chave, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe evento para a chave de idempotencia no tenant.</summary>
    /// <param name="chave">Chave de negocio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir.</returns>
    Task<bool> ExisteParaChaveAsync(ChaveIdempotenciaEvento chave, CancellationToken cancellationToken);

    /// <summary>Lista eventos num estado (ex.: <c>Assinado</c> para empacotar; <c>Transmitido</c> para consultar).</summary>
    /// <param name="estado">Estado alvo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Eventos no estado, ordenados por geracao.</returns>
    Task<IReadOnlyList<EventoESocial>> ListarPorEstadoAsync(EstadoEventoESocial estado, CancellationToken cancellationToken);

    /// <summary>Lista todos os eventos do tenant (respeitando o Global Query Filter), ordenados por geracao.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Eventos do tenant, ordenados por geracao.</returns>
    Task<IReadOnlyList<EventoESocial>> ListarTodosAsync(CancellationToken cancellationToken);
}
