using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

namespace Tensorroot.Gov.Platform.Tenancy;

/// <summary>
/// Protege a connection string do banco DEDICADO de cada tenant EM REPOUSO no catálogo de CONTROLE
/// (SEC-1), reusando o MESMO padrão de ENVELOPE ENCRYPTION do Cofre (A1-DESIGN §1/§3): gera uma DEK
/// AES-256 por segredo, cifra a string com AES-256-GCM (nonce único por operação,
/// AAD=TenantId||"connstring" para amarrar o blob ao tenant) e embrulha (wrap) a DEK na KEK via
/// <see cref="IProvedorKek"/> (Key Vault/HSM em PROD; config FORA do repo em DEV). A KEK NUNCA toca
/// o código nem o banco. A decifragem acontece SÓ EM MEMÓRIA ao resolver a conexão; nada em claro é
/// persistido. A tag GCM autentica: adulteração/cross-tenant na coluna faz a decifragem LANÇAR.
/// </summary>
public sealed class ProtetorConexaoTenant(IProvedorKek provedorKek)
{
    /// <summary>Prefixo do envelope versionado — distingue um valor protegido de um legado em claro.</summary>
    public const string Prefixo = "encv1:";

    private const int TamanhoDek = 32;
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;

    private readonly IProvedorKek _provedorKek = provedorKek;

    /// <summary>
    /// Indica se <paramref name="valor"/> já está protegido (tem o envelope versionado). Strings em
    /// claro herdadas (migração) NÃO têm o prefixo e devem ser cifradas na primeira escrita.
    /// </summary>
    /// <param name="valor">Valor persistido na coluna.</param>
    /// <returns><c>true</c> se já é um envelope protegido.</returns>
    public static bool EstaProtegida(string? valor)
        => valor is not null && valor.StartsWith(Prefixo, StringComparison.Ordinal);

    /// <summary>
    /// Cifra a connection string em claro num envelope autocontido (Base64): wrap(DEK)‖nonce‖tag‖
    /// cipher‖kekKeyId, amarrado ao tenant pela AAD. Zera todo material em claro no finally.
    /// </summary>
    /// <param name="tenantId">Tenant dono da conexão (compõe a AAD).</param>
    /// <param name="conexaoEmClaro">Connection string em claro.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Envelope protegido (prefixado por <see cref="Prefixo"/>).</returns>
    public async Task<string> ProtegerAsync(Guid tenantId, string conexaoEmClaro, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conexaoEmClaro);

        byte[] dek = RandomNumberGenerator.GetBytes(TamanhoDek);
        byte[] plano = Encoding.UTF8.GetBytes(conexaoEmClaro);
        try
        {
            var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
            var cipher = new byte[plano.Length];
            var tag = new byte[TamanhoTag];

            using (var gcm = new AesGcm(dek, TamanhoTag))
            {
                gcm.Encrypt(nonce, plano, cipher, tag, MontarAad(tenantId));
            }

            var dekWrapped = await _provedorKek.EnvolverDekAsync(dek, cancellationToken).ConfigureAwait(false);
            var kekKeyId = Encoding.UTF8.GetBytes(_provedorKek.KekKeyId);

            // Layout autocontido: [len(dekWrapped)][dekWrapped][nonce][tag][len(kekKeyId)][kekKeyId][cipher].
            var saida = new byte[4 + dekWrapped.Length + TamanhoNonce + TamanhoTag + 4 + kekKeyId.Length + cipher.Length];
            var pos = 0;
            BinaryPrimitives.WriteInt32LittleEndian(saida.AsSpan(pos, 4), dekWrapped.Length);
            pos += 4;
            Buffer.BlockCopy(dekWrapped, 0, saida, pos, dekWrapped.Length);
            pos += dekWrapped.Length;
            Buffer.BlockCopy(nonce, 0, saida, pos, TamanhoNonce);
            pos += TamanhoNonce;
            Buffer.BlockCopy(tag, 0, saida, pos, TamanhoTag);
            pos += TamanhoTag;
            BinaryPrimitives.WriteInt32LittleEndian(saida.AsSpan(pos, 4), kekKeyId.Length);
            pos += 4;
            Buffer.BlockCopy(kekKeyId, 0, saida, pos, kekKeyId.Length);
            pos += kekKeyId.Length;
            Buffer.BlockCopy(cipher, 0, saida, pos, cipher.Length);

            return Prefixo + Convert.ToBase64String(saida);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
            CryptographicOperations.ZeroMemory(plano);
        }
    }

    /// <summary>
    /// Decifra um envelope protegido SÓ EM MEMÓRIA, devolvendo a connection string em claro. Lança
    /// <see cref="InvalidOperationException"/> se a integridade falhar (adulteração/AAD divergente/
    /// KEK incorreta) — o chamador NUNCA deve abrir conexão com material suspeito. Zera a DEK no finally.
    /// </summary>
    /// <param name="tenantId">Tenant esperado (deve coincidir com a AAD da cifragem).</param>
    /// <param name="envelope">Valor protegido (prefixado por <see cref="Prefixo"/>).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Connection string em claro.</returns>
    public async Task<string> RevelarAsync(Guid tenantId, string envelope, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope);
        if (!EstaProtegida(envelope))
        {
            throw new InvalidOperationException("Connection string não está no formato de envelope protegido (SEC-1).");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(envelope[Prefixo.Length..]);
        }
        catch (FormatException excecao)
        {
            throw new InvalidOperationException("Envelope de connection string corrompido (Base64 inválido).", excecao);
        }

        byte[] dek = [];
        byte[] plano = [];
        try
        {
            var pos = 0;
            var tamanhoDekWrapped = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(pos, 4));
            pos += 4;
            var dekWrapped = bytes.AsSpan(pos, tamanhoDekWrapped).ToArray();
            pos += tamanhoDekWrapped;
            var nonce = bytes.AsSpan(pos, TamanhoNonce).ToArray();
            pos += TamanhoNonce;
            var tag = bytes.AsSpan(pos, TamanhoTag).ToArray();
            pos += TamanhoTag;
            var tamanhoKekKeyId = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(pos, 4));
            pos += 4;
            var kekKeyId = Encoding.UTF8.GetString(bytes.AsSpan(pos, tamanhoKekKeyId));
            pos += tamanhoKekKeyId;
            var cipher = bytes.AsSpan(pos).ToArray();

            dek = await _provedorKek.RevelarDekAsync(dekWrapped, kekKeyId, cancellationToken).ConfigureAwait(false);

            plano = new byte[cipher.Length];
            using (var gcm = new AesGcm(dek, TamanhoTag))
            {
                gcm.Decrypt(nonce, cipher, tag, plano, MontarAad(tenantId));
            }

            return Encoding.UTF8.GetString(plano);
        }
        catch (Exception excecao) when (excecao is CryptographicException or ArgumentException or ArgumentOutOfRangeException)
        {
            // Tag GCM não confere (adulteração/cross-tenant), KEK incorreta ou layout corrompido.
            throw new InvalidOperationException("Falha de integridade ao decifrar a connection string do tenant (SEC-1).", excecao);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
            CryptographicOperations.ZeroMemory(plano);
        }
    }

    // AAD = TenantId||"connstring": amarra o blob ao tenant (cross-tenant faz a decifragem falhar).
    private static byte[] MontarAad(Guid tenantId)
        => Encoding.ASCII.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{tenantId:N}|connstring"));
}
