using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

/// <summary>Resultado de uma cifragem de cadastro (entidade pronta + nada em claro retido).</summary>
/// <param name="Titular">CN/Razao Social extraido do certificado.</param>
/// <param name="Thumbprint">Thumbprint SHA-256 (hex).</param>
/// <param name="NotBeforeUtc">Inicio da validade (UTC).</param>
/// <param name="NotAfterUtc">Fim da validade (UTC).</param>
/// <param name="Pfx">Bloco AES-GCM do .pfx.</param>
/// <param name="Senha">Bloco AES-GCM da senha.</param>
/// <param name="DekWrapped">DEK embrulhada pela KEK.</param>
/// <param name="KekKeyId">Identificador/versao da KEK.</param>
internal sealed record CifragemResultado(
    string Titular,
    string Thumbprint,
    DateTime NotBeforeUtc,
    DateTime NotAfterUtc,
    MaterialCifrado Pfx,
    MaterialCifrado Senha,
    byte[] DekWrapped,
    string KekKeyId);

/// <summary>
/// Nucleo criptografico do cofre (A1-DESIGN §3): cifra o .pfx+senha no cadastro/rotacao (envelope) e
/// decifra SO-EM-MEMORIA na assinatura, carregando o X509 com <see cref="X509KeyStorageFlags.EphemeralKeySet"/>
/// (a chave NUNCA vai para o keystore do SO/container — risco 3) e zerando DEK/senha/pfx no finally
/// (risco 6). A chave privada NUNCA e exportada, serializada, logada ou retornada.
/// </summary>
internal sealed class CofreCripto(IProvedorKek provedorKek)
{
    /// <summary>
    /// Valida o .pfx (carrega para conferir serie/validade/cadeia), gera DEK, cifra .pfx+senha
    /// (AES-256-GCM, AAD=TenantId||Thumbprint), embrulha a DEK na KEK e zera todo material em claro.
    /// </summary>
    /// <param name="tenantId">Tenant dono do certificado (compoe a AAD e valida o CNPJ).</param>
    /// <param name="pfxBytes">Bytes do .pfx (recebido so em memoria).</param>
    /// <param name="senha">Senha do .pfx (so em memoria).</param>
    /// <param name="agoraUtc">Instante atual (UTC).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Material cifrado e metadados para a entidade do cofre.</returns>
    /// <exception cref="CertificadoInvalidoException">Serie/validade/cadeia/CNPJ invalidos.</exception>
    public async Task<CifragemResultado> CifrarParaCadastroAsync(
        Guid tenantId,
        byte[] pfxBytes,
        string senha,
        DateTime agoraUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pfxBytes);

        byte[] dek = [];
        byte[] senhaBytes = Encoding.UTF8.GetBytes(senha);
        X509Certificate2? cert = null;
        try
        {
            // EphemeralKeySet: a chave privada NAO toca o keystore do SO/container (A1-DESIGN §7 risco 3).
            cert = CarregarPkcs12Efemero(pfxBytes, senha);

            if (cert.NotAfter.ToUniversalTime() <= agoraUtc)
            {
                throw new CertificadoInvalidoException("Certificado A1 expirado (NotAfter no passado).");
            }

            ValidacaoCadeiaIcpBrasil.Validar(cert);

            var thumbprint = cert.GetCertHashString(HashAlgorithmName.SHA256);
            var titular = ExtrairTitular(cert);
            var aad = MontarAad(tenantId, thumbprint);

            dek = AesGcmEnvelope.GerarDek();
            var pfxCifrado = AesGcmEnvelope.Cifrar(dek, pfxBytes, aad);
            var senhaCifrada = AesGcmEnvelope.Cifrar(dek, senhaBytes, aad);
            var dekWrapped = await provedorKek.EnvolverDekAsync(dek, cancellationToken).ConfigureAwait(false);

            return new CifragemResultado(
                titular,
                thumbprint,
                cert.NotBefore.ToUniversalTime(),
                cert.NotAfter.ToUniversalTime(),
                pfxCifrado,
                senhaCifrada,
                dekWrapped,
                provedorKek.KekKeyId);
        }
        catch (CryptographicException excecao)
        {
            // .pfx invalido ou senha incorreta — NUNCA vazar material na mensagem.
            throw new CertificadoInvalidoException("Falha ao carregar o certificado A1 (.pfx invalido ou senha incorreta).", excecao);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
            CryptographicOperations.ZeroMemory(senhaBytes);
            CryptographicOperations.ZeroMemory(pfxBytes);
            cert?.Dispose();
        }
    }

    /// <summary>
    /// Decifra o .pfx+senha SO-EM-MEMORIA e executa <paramref name="operacao"/> com o
    /// <see cref="X509Certificate2"/> carregado em <see cref="X509KeyStorageFlags.EphemeralKeySet"/>.
    /// Garante Dispose do cert e ZeroMemory de DEK/senha/pfx no finally (A1-DESIGN §3.2). A chave
    /// privada existe apenas durante a operacao e NUNCA e retornada/serializada.
    /// </summary>
    /// <typeparam name="T">Tipo do resultado da operacao de assinatura.</typeparam>
    /// <param name="certificado">Registro do cofre (material cifrado).</param>
    /// <param name="operacao">Operacao que usa o cert e sua chave privada (assinar).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O resultado produzido pela operacao.</returns>
    public async Task<T> UsarCertificadoAsync<T>(
        CertificadoA1Cofre certificado,
        Func<X509Certificate2, T> operacao,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(certificado);
        ArgumentNullException.ThrowIfNull(operacao);

        byte[] dek = [];
        byte[] senhaBytes = [];
        byte[] pfxBytes = [];
        X509Certificate2? cert = null;
        try
        {
            dek = await provedorKek
                .RevelarDekAsync(certificado.DekWrapped, certificado.KekKeyId, cancellationToken)
                .ConfigureAwait(false);

            var aad = MontarAad(certificado.TenantId, certificado.Thumbprint);
            pfxBytes = AesGcmEnvelope.Decifrar(dek, new MaterialCifrado(certificado.PfxCipher, certificado.PfxNonce, certificado.PfxTag), aad);
            senhaBytes = AesGcmEnvelope.Decifrar(dek, new MaterialCifrado(certificado.SenhaCipher, certificado.SenhaNonce, certificado.SenhaTag), aad);

            var senha = Encoding.UTF8.GetString(senhaBytes);
            cert = CarregarPkcs12Efemero(pfxBytes, senha);

            return operacao(cert);
        }
        catch (CryptographicException excecao)
        {
            // Tag GCM nao confere (adulteracao/cross-tenant) ou pfx corrompido (A1-DESIGN §7 risco 7).
            throw new CertificadoInvalidoException("Falha de integridade ao decifrar o certificado A1 (material adulterado ou KEK incorreta).", excecao);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
            CryptographicOperations.ZeroMemory(senhaBytes);
            CryptographicOperations.ZeroMemory(pfxBytes);
            cert?.Dispose();
        }
    }

    /// <summary>
    /// Carrega o .pfx com <see cref="X509KeyStorageFlags.EphemeralKeySet"/>: a chave privada vive so
    /// em memoria e NAO e gravada no keystore do SO/container (A1-DESIGN §3/§7 risco 3). No TFM net8
    /// usa-se o construtor (SYSLIB0057): // TODO(validar-oficial): migrar para X509CertificateLoader
    /// ao subir o TFM (net9+). A supressao e LOCAL e justificada.
    /// </summary>
    private static X509Certificate2 CarregarPkcs12Efemero(byte[] pfxBytes, string senha)
    {
#pragma warning disable SYSLIB0057 // X509CertificateLoader indisponivel no TFM net8; ctor e a API vigente.
        try
        {
            // PRODUCAO (Linux/Windows): EphemeralKeySet — a chave NAO toca o keystore do SO/container.
            return new X509Certificate2(pfxBytes, senha, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (PlatformNotSupportedException)
        {
            // macOS (dev) NAO suporta EphemeralKeySet. Fallback so-em-memoria (sem persistir no
            // keychain) apenas para desenvolvimento local; PROD roda em container Linux (A1-DESIGN §8).
            return new X509Certificate2(pfxBytes, senha, X509KeyStorageFlags.Exportable);
        }
#pragma warning restore SYSLIB0057
    }

    /// <summary>AAD = TenantId||Thumbprint, amarrando o blob cifrado ao tenant (A1-DESIGN §7 risco 7).</summary>
    private static byte[] MontarAad(Guid tenantId, string thumbprint)
        => Encoding.ASCII.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{tenantId:N}|{thumbprint}"));

    private static string ExtrairTitular(X509Certificate2 cert)
    {
        var cn = cert.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
        return string.IsNullOrWhiteSpace(cn) ? cert.Subject : cn;
    }
}
