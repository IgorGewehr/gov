using System.Security.Cryptography;
using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

/// <summary>
/// Primitiva AES-256-GCM (AEAD) do envelope encryption (A1-DESIGN §1/§3). Gera nonce ALEATORIO de
/// 12 bytes UNICO por cifragem (A1-DESIGN §7, risco 1: reuso de nonce com a mesma DEK quebra a
/// confidencialidade) e usa AAD = TenantId||Thumbprint para amarrar o blob ao tenant (risco 7).
/// A tag GCM autentica: adulteracao no banco faz a decifragem LANCAR (nao decifra).
/// </summary>
internal static class AesGcmEnvelope
{
    /// <summary>Tamanho da DEK/chave AES-256 em bytes.</summary>
    public const int TamanhoChave = 32;

    /// <summary>Gera uma DEK AES-256 aleatoria (32 bytes). O chamador deve zera-la apos o uso.</summary>
    /// <returns>DEK em claro.</returns>
    public static byte[] GerarDek() => RandomNumberGenerator.GetBytes(TamanhoChave);

    /// <summary>Cifra um segredo com a DEK, produzindo um <see cref="MaterialCifrado"/> (nonce unico).</summary>
    /// <param name="dek">DEK AES-256 em claro (32 bytes).</param>
    /// <param name="plano">Segredo em claro a cifrar (.pfx ou senha).</param>
    /// <param name="aad">Dado autenticado adicional (TenantId||Thumbprint).</param>
    /// <returns>Bloco cifrado (cipher + nonce + tag).</returns>
    public static MaterialCifrado Cifrar(ReadOnlySpan<byte> dek, ReadOnlySpan<byte> plano, ReadOnlySpan<byte> aad)
    {
        var nonce = RandomNumberGenerator.GetBytes(MaterialCifrado.TamanhoNonce);
        var cipher = new byte[plano.Length];
        var tag = new byte[MaterialCifrado.TamanhoTag];

        using var gcm = new AesGcm(dek, MaterialCifrado.TamanhoTag);
        gcm.Encrypt(nonce, plano, cipher, tag, aad);

        return new MaterialCifrado(cipher, nonce, tag);
    }

    /// <summary>
    /// Decifra um <see cref="MaterialCifrado"/> com a DEK. Lanca <see cref="CryptographicException"/>
    /// se a tag nao conferir (adulteracao/AAD divergente) — o chamador deve tratar como falha de
    /// integridade e NUNCA assinar.
    /// </summary>
    /// <param name="dek">DEK AES-256 em claro.</param>
    /// <param name="material">Bloco cifrado.</param>
    /// <param name="aad">Dado autenticado adicional (TenantId||Thumbprint) — deve coincidir com a cifragem.</param>
    /// <returns>O segredo em claro (o chamador deve zera-lo apos o uso).</returns>
    public static byte[] Decifrar(ReadOnlySpan<byte> dek, MaterialCifrado material, ReadOnlySpan<byte> aad)
    {
        ArgumentNullException.ThrowIfNull(material);
        var plano = new byte[material.Cipher.Length];
        using var gcm = new AesGcm(dek, MaterialCifrado.TamanhoTag);
        gcm.Decrypt(material.Nonce, material.Cipher, material.Tag, plano, aad);
        return plano;
    }
}
