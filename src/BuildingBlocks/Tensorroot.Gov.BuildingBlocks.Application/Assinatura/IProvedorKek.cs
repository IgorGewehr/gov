namespace Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

/// <summary>
/// PORTA da chave-mestra (KEK — Key Encryption Key) que embrulha (wrap) a DEK de cada certificado
/// (envelope encryption — A1-DESIGN §1). Em PRODUCAO a KEK vive no Azure Key Vault/HSM: as operacoes
/// wrap/unwrap acontecem DENTRO do cofre e a chave-mestra NUNCA sai do HSM. Em DEV a KEK vem de
/// config (fora do repo) — // TODO(prod: Key Vault wrap/unwrap). A KEK em si nunca trafega por aqui.
/// </summary>
public interface IProvedorKek
{
    /// <summary>Identificador/versao da chave-mestra corrente (persistido para rotacao — A1-DESIGN §2 KekKeyId).</summary>
    string KekKeyId { get; }

    /// <summary>Embrulha (wrap) a DEK em claro com a KEK, devolvendo a DEK cifrada para persistir.</summary>
    /// <param name="dek">DEK (Data Encryption Key) AES-256 em claro (32 bytes).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A DEK embrulhada (wrapped) pela KEK.</returns>
    Task<byte[]> EnvolverDekAsync(ReadOnlyMemory<byte> dek, CancellationToken cancellationToken);

    /// <summary>Desembrulha (unwrap) a DEK cifrada com a KEK identificada, devolvendo a DEK em claro.</summary>
    /// <param name="dekEmbrulhada">DEK previamente embrulhada pela KEK.</param>
    /// <param name="kekKeyId">Identificador/versao da KEK que embrulhou a DEK (para rotacao).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A DEK em claro (o chamador deve zera-la apos o uso).</returns>
    Task<byte[]> RevelarDekAsync(ReadOnlyMemory<byte> dekEmbrulhada, string kekKeyId, CancellationToken cancellationToken);
}
