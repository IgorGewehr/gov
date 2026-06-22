using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

/// <summary>
/// Assinatura CMS/PKCS#7 (CAdES) para artefatos NAO-XML/.TXT (A1-DESIGN §4.2). [a confirmar] onde o
/// TCE/SICONFI exigir assinatura embutida — // TODO(validar-oficial: Manual PAD TCE-RS / Manual
/// SICONFI). Digest SHA-256.
/// </summary>
internal static class AssinadorCms
{
    /// <summary>Produz o envelope CMS (DER) assinado com o certificado A1 (chave privada efemera).</summary>
    /// <param name="conteudo">Conteudo a assinar.</param>
    /// <param name="cert">Certificado A1 (com chave privada efemera).</param>
    /// <param name="detached">Verdadeiro para assinatura destacada; falso para anexada.</param>
    /// <returns>Envelope CMS codificado (DER).</returns>
    public static byte[] Assinar(ReadOnlyMemory<byte> conteudo, X509Certificate2 cert, bool detached)
    {
        ArgumentNullException.ThrowIfNull(cert);

        var conteudoInfo = new ContentInfo(conteudo.ToArray());
        var cms = new SignedCms(conteudoInfo, detached);
        var signer = new CmsSigner(cert)
        {
            DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"), // SHA-256
            IncludeOption = X509IncludeOption.EndCertOnly,
        };

        cms.ComputeSignature(signer, silent: true);
        return cms.Encode();
    }
}
