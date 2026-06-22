using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Anti-Corruption Layer (saida) do e-SUS APS / SISAB (Portaria 1.412/2013): envio da producao (CDS)
/// na competencia. Idempotente por <c>(AtendimentoId, Competencia)</c>, com timeout, retry e circuit
/// breaker (Polly). A implementacao concreta vive na Infrastructure.
/// </summary>
public interface ISisabGateway
{
    /// <summary>Envia a producao do atendimento ao SISAB na competencia informada.</summary>
    /// <param name="atendimentoId">Identificador do atendimento (chave de idempotencia).</param>
    /// <param name="competencia">Competencia da producao.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa concluida ao confirmar o envio.</returns>
    Task EnviarProducaoAsync(AtendimentoId atendimentoId, Competencia competencia, CancellationToken cancellationToken);
}
