using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;

/// <summary>
/// Assinatura XML-DSig Enveloped (A1-DESIGN §4.1). Para eSocial fixa RSA-SHA256 + digest SHA-256 +
/// canonicalizacao INCLUSIVA (REC-xml-c14n-20010315 — NAO o default exc-c14n do .NET, A1-DESIGN §7
/// risco 4) + KeyInfo EndCertOnly (so &lt;X509Certificate&gt;). FONTE: Manual Desenvolvedor eSocial
/// v1.15 §6.7 (CONFIRMADO). // TODO(validar-oficial): cruzar com MOS S-1.3 vigente no M5.
/// </summary>
internal static class AssinadorXmlDsig
{
    private const string C14NInclusiva = SignedXml.XmlDsigC14NTransformUrl;
    private const string DigestSha256 = "http://www.w3.org/2001/04/xmlenc#sha256";
    private const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

    /// <summary>
    /// Assina o documento XML com o certificado e a chave privada efemera, devolvendo os bytes UTF-8
    /// do XML assinado. A chave privada e usada somente aqui e nao e retida.
    /// </summary>
    /// <param name="xmlUtf8">XML a assinar (UTF-8).</param>
    /// <param name="opcoes">Opcoes/destino (selecionam o perfil C14N/KeyInfo).</param>
    /// <param name="cert">Certificado A1 (com chave privada efemera).</param>
    /// <returns>Bytes UTF-8 do XML assinado.</returns>
    /// <exception cref="CertificadoInvalidoException">Se o certificado nao tiver chave RSA utilizavel.</exception>
    public static byte[] Assinar(ReadOnlyMemory<byte> xmlUtf8, OpcoesAssinaturaXml opcoes, X509Certificate2 cert)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        ArgumentNullException.ThrowIfNull(cert);

        var documento = new XmlDocument { PreserveWhitespace = true };
        using (var leitor = XmlReader.Create(
            new MemoryStream(xmlUtf8.ToArray()),
            new XmlReaderSettings { XmlResolver = null, DtdProcessing = DtdProcessing.Prohibit }))
        {
            documento.Load(leitor);
        }

        using var rsa = cert.GetRSAPrivateKey()
            ?? throw new CertificadoInvalidoException("Certificado A1 sem chave privada RSA utilizavel.");

        var signedXml = new SignedXmlComId(documento) { SigningKey = rsa };
        signedXml.SignedInfo!.CanonicalizationMethod = C14NInclusiva; // INCLUSIVA (eSocial)
        signedXml.SignedInfo.SignatureMethod = RsaSha256;

        var referencia = new Reference { Uri = opcoes.ReferenceUri, DigestMethod = DigestSha256 };
        referencia.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        referencia.AddTransform(new XmlDsigC14NTransform()); // C14N inclusiva, nao exc-c14n
        signedXml.AddReference(referencia);

        // KeyInfo EndCertOnly: somente <X509Certificate> (A1-DESIGN §4.1).
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();
        var assinatura = signedXml.GetXml();
        documento.DocumentElement!.AppendChild(documento.ImportNode(assinatura, deep: true));

        using var saida = new MemoryStream();
        using (var escritor = XmlWriter.Create(saida, new XmlWriterSettings { Encoding = new System.Text.UTF8Encoding(false) }))
        {
            documento.Save(escritor);
        }

        return saida.ToArray();
    }

    /// <summary>
    /// Subclasse que resolve referencias por atributo <c>Id</c> (sem depender do schema), necessaria
    /// quando o XML referencia um elemento por "#Id" (eSocial). Sem isso o SignedXml do .NET pode nao
    /// localizar o elemento e produzir assinatura invalida.
    /// </summary>
    private sealed class SignedXmlComId(XmlDocument documento) : SignedXml(documento)
    {
        public override XmlElement? GetIdElement(XmlDocument? document, string idValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(idValue);
            var encontrado = base.GetIdElement(document, idValue);
            if (encontrado is not null || document is null)
            {
                return encontrado;
            }

            foreach (XmlElement elemento in document.SelectNodes("//*[@Id]")!.Cast<XmlElement>())
            {
                if (string.Equals(elemento.GetAttribute("Id"), idValue, StringComparison.Ordinal))
                {
                    return elemento;
                }
            }

            return null;
        }
    }
}
