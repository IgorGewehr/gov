using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Anti-Corruption Layer (saida) da RNDS (Portaria 1.434/2020): envio do RES como Bundle HL7 FHIR R4
/// via mTLS + ICP-Brasil. Idempotente por identificador do Bundle/atendimento, com timeout, retry e
/// circuit breaker (Polly). A implementacao concreta vive na Infrastructure.
/// </summary>
public interface IRndsGateway
{
    /// <summary>Monta e envia o Bundle FHIR R4 do atendimento a RNDS e retorna o protocolo de aceite.</summary>
    /// <param name="atendimentoId">Identificador do atendimento (chave de idempotencia).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Protocolo de aceite retornado pela RNDS.</returns>
    Task<string> EnviarBundleAsync(AtendimentoId atendimentoId, CancellationToken cancellationToken);
}
