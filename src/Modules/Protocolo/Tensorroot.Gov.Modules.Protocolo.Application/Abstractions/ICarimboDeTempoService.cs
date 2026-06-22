using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;

/// <summary>
/// Servico de autoridade de carimbo de tempo confiavel (Lei 14.063/2020): atesta o instante da
/// assinatura, produzindo um <see cref="CarimboDeTempo"/>. A I/O externa deve ser idempotente e
/// resiliente (timeout, retry e circuit breaker — Polly), atras de Anti-Corruption Layer.
/// </summary>
public interface ICarimboDeTempoService
{
    /// <summary>Gera um carimbo de tempo confiavel para o instante corrente da assinatura.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Carimbo de tempo atestado pela autoridade.</returns>
    Task<CarimboDeTempo> GerarAsync(CancellationToken cancellationToken);
}
