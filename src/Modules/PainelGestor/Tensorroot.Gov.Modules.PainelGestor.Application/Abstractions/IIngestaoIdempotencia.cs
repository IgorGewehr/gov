namespace Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;

/// <summary>
/// Porta da idempotência da ingestão (ACL de entrada): verifica/registra o <c>EventId</c> de Integration
/// Events já consumidos, por tenant, para que os indicadores acumuladores não contem o mesmo fato duas
/// vezes na reentrega at-least-once do Outbox (I-13). Tenant-scoped pelo Global Query Filter.
/// </summary>
public interface IIngestaoIdempotencia
{
    /// <summary>Indica se o evento já foi ingerido (por <c>EventId</c>) neste tenant.</summary>
    /// <param name="eventId">EventId do Integration Event.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já processado; caso contrário, <c>false</c>.</returns>
    Task<bool> JaProcessadoAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Registra o evento como ingerido (na mesma unidade de trabalho da materialização).</summary>
    /// <param name="eventId">EventId do Integration Event.</param>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="tipoEvento">Nome do tipo do evento.</param>
    void Registrar(Guid eventId, Guid tenantId, string tipoEvento);
}
