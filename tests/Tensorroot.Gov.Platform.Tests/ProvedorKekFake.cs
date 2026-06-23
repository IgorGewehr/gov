using System.Security.Cryptography;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.Platform.Tests;

/// <summary>
/// KEK de teste (SEC-1): embrulha/desembrulha a DEK com AES-256-GCM e uma chave fixa em memória —
/// suficiente para exercitar o envelope de connection string sem Key Vault. NUNCA usar em produção.
/// </summary>
internal sealed class ProvedorKekFake : IProvedorKek
{
    private static readonly byte[] Kek = RandomNumberGenerator.GetBytes(32);
    private static readonly byte[] Aad = System.Text.Encoding.ASCII.GetBytes("teste.kek.v1");
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;

    public string KekKeyId => "teste-kek-1";

    public Task<byte[]> EnvolverDekAsync(ReadOnlyMemory<byte> dek, CancellationToken cancellationToken)
    {
        var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
        var cipher = new byte[dek.Length];
        var tag = new byte[TamanhoTag];
        using var gcm = new AesGcm(Kek, TamanhoTag);
        gcm.Encrypt(nonce, dek.Span, cipher, tag, Aad);

        var saida = new byte[TamanhoNonce + TamanhoTag + cipher.Length];
        Buffer.BlockCopy(nonce, 0, saida, 0, TamanhoNonce);
        Buffer.BlockCopy(tag, 0, saida, TamanhoNonce, TamanhoTag);
        Buffer.BlockCopy(cipher, 0, saida, TamanhoNonce + TamanhoTag, cipher.Length);
        return Task.FromResult(saida);
    }

    public Task<byte[]> RevelarDekAsync(ReadOnlyMemory<byte> dekEmbrulhada, string kekKeyId, CancellationToken cancellationToken)
    {
        var dados = dekEmbrulhada.Span;
        var nonce = dados[..TamanhoNonce].ToArray();
        var tag = dados.Slice(TamanhoNonce, TamanhoTag).ToArray();
        var cipher = dados[(TamanhoNonce + TamanhoTag)..].ToArray();
        var dek = new byte[cipher.Length];
        using var gcm = new AesGcm(Kek, TamanhoTag);
        gcm.Decrypt(nonce, cipher, tag, dek, Aad);
        return Task.FromResult(dek);
    }

    /// <summary>Cria um <see cref="ProtetorConexaoTenant"/> sobre esta KEK de teste.</summary>
    public static ProtetorConexaoTenant Protetor() => new(new ProvedorKekFake());
}
