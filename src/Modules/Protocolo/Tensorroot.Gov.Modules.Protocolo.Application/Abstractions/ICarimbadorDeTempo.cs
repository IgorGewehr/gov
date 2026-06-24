using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;

/// <summary>
/// Autoridade de Carimbo do Tempo (ACT) credenciada ICP-Brasil (RFC 3161 / DOC-ICP-12 v2.1).
/// Recebe o HASH do documento (nunca o conteudo — privacidade: a ACT nao ve o arquivo) e devolve o
/// TST validado vinculado a esse hash. I/O idempotente (nonce), resiliente (Polly) e atras de ACL.
/// <para>
/// W9.4 (Peca 1): substitui <see cref="ICarimboDeTempoService"/> (que nao recebia o hash de entrada).
/// O <see cref="ICarimboDeTempoService"/> permanece como contrato legado do carimbo local.
/// </para>
/// </summary>
public interface ICarimbadorDeTempo
{
    /// <summary>Solicita um carimbo de tempo confiavel para o hash informado.</summary>
    /// <param name="hashDocumento">SHA-256 (hex) do conteudo a carimbar — o MessageImprint do TSQ.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Carimbo de tempo validado e vinculado ao hash.</returns>
    Task<CarimboDeTempo> CarimbarAsync(Hash hashDocumento, CancellationToken cancellationToken);
}
