using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

/// <summary>
/// Provedor de KEK para DEV (sem Key Vault): a KEK vem de config (Base64) FORA do repo. O wrap/unwrap
/// e feito com AES-256-GCM localmente, com o MESMO algoritmo de envelope de PROD — so muda a ORIGEM
/// da KEK (A1-DESIGN §1). // TODO(prod: Key Vault wrap/unwrap) — NUNCA hardcode a KEK no codigo.
/// </summary>
internal sealed class ProvedorKekConfig : IProvedorKek
{
    private const string AadDek = "tensorroot.cofre.dek-wrap.v1";

    private readonly byte[] _kek;

    /// <summary>Inicializa o provedor a partir das opcoes do cofre.</summary>
    /// <param name="options">Opcoes do cofre (com a KEK em Base64).</param>
    /// <exception cref="InvalidOperationException">Se a KEK nao estiver configurada ou tiver tamanho invalido.</exception>
    public ProvedorKekConfig(IOptions<CofreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var valores = options.Value;

        if (string.IsNullOrWhiteSpace(valores.KekBase64))
        {
            throw new InvalidOperationException(
                "Cofre:KekBase64 nao configurada. Defina a KEK de DEV (Base64, 32 bytes) FORA do repo " +
                "(variavel de ambiente/secret manager). Em PRODUCAO use o provedor KeyVault.");
        }

        _kek = Convert.FromBase64String(valores.KekBase64);
        if (_kek.Length != AesGcmEnvelope.TamanhoChave)
        {
            throw new InvalidOperationException("Cofre:KekBase64 deve conter exatamente 32 bytes (AES-256).");
        }

        KekKeyId = valores.KekKeyIdDev;
    }

    /// <inheritdoc />
    public string KekKeyId { get; }

    /// <inheritdoc />
    public Task<byte[]> EnvolverDekAsync(ReadOnlyMemory<byte> dek, CancellationToken cancellationToken)
    {
        var aad = System.Text.Encoding.ASCII.GetBytes(AadDek);
        var bloco = AesGcmEnvelope.Cifrar(_kek, dek.Span, aad);
        // Layout: nonce(12) || tag(16) || cipher. Autocontido para o unwrap.
        var saida = new byte[bloco.Nonce.Length + bloco.Tag.Length + bloco.Cipher.Length];
        Buffer.BlockCopy(bloco.Nonce, 0, saida, 0, bloco.Nonce.Length);
        Buffer.BlockCopy(bloco.Tag, 0, saida, bloco.Nonce.Length, bloco.Tag.Length);
        Buffer.BlockCopy(bloco.Cipher, 0, saida, bloco.Nonce.Length + bloco.Tag.Length, bloco.Cipher.Length);
        return Task.FromResult(saida);
    }

    /// <inheritdoc />
    public Task<byte[]> RevelarDekAsync(ReadOnlyMemory<byte> dekEmbrulhada, string kekKeyId, CancellationToken cancellationToken)
    {
        var dados = dekEmbrulhada.Span;
        var nonce = dados[..Domain.MaterialCifrado.TamanhoNonce].ToArray();
        var tag = dados.Slice(Domain.MaterialCifrado.TamanhoNonce, Domain.MaterialCifrado.TamanhoTag).ToArray();
        var cipher = dados[(Domain.MaterialCifrado.TamanhoNonce + Domain.MaterialCifrado.TamanhoTag)..].ToArray();
        var aad = System.Text.Encoding.ASCII.GetBytes(AadDek);

        var dek = AesGcmEnvelope.Decifrar(_kek, new Domain.MaterialCifrado(cipher, nonce, tag), aad);
        return Task.FromResult(dek);
    }
}
