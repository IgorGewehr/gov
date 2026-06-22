using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using FluentAssertions;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;
using Xunit;

namespace Tensorroot.Gov.Modules.Cofre.Tests;

/// <summary>
/// Testes da assinatura XML-DSig (A1-DESIGN §4.1/§8): a assinatura produzida e VALIDA (verifica com a
/// chave publica), usa C14N INCLUSIVA (nao exc-c14n) e RSA-SHA256, e inclui o &lt;X509Certificate&gt;.
/// </summary>
public sealed class AssinaturaXmlDsigTests
{
    [Fact]
    public void Assina_xml_e_a_assinatura_e_valida()
    {
        using var cert = CarregarCertComChave();
        var xml = Encoding.UTF8.GetBytes("<eSocial><evtInfoEmpregador Id=\"ID1\"><x>1</x></evtInfoEmpregador></eSocial>");

        var assinado = AssinadorXmlDsig.Assinar(xml, new OpcoesAssinaturaXml(DestinoAssinatura.ESocial, "#ID1"), cert);

        var documento = new XmlDocument { PreserveWhitespace = true };
        documento.LoadXml(Encoding.UTF8.GetString(assinado));
        var nos = documento.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
        nos.Count.Should().BeGreaterThan(0);

        var signedXml = new SignedXml(documento);
        signedXml.LoadXml((XmlElement)nos[0]!);
        signedXml.CheckSignature(cert, verifySignatureOnly: true).Should().BeTrue();
    }

    [Fact]
    public void Assinatura_usa_c14n_inclusiva_e_rsa_sha256()
    {
        using var cert = CarregarCertComChave();
        var xml = Encoding.UTF8.GetBytes("<doc><item>conteudo</item></doc>");

        var assinado = AssinadorXmlDsig.Assinar(xml, new OpcoesAssinaturaXml(DestinoAssinatura.ESocial), cert);
        var texto = Encoding.UTF8.GetString(assinado);

        // C14N INCLUSIVA (REC-xml-c14n-20010315), NUNCA exc-c14n (A1-DESIGN §7 risco 4).
        texto.Should().Contain("http://www.w3.org/TR/2001/REC-xml-c14n-20010315");
        texto.Should().NotContain("xml-exc-c14n");
        texto.Should().Contain("xmldsig-more#rsa-sha256");
        texto.Should().Contain("X509Certificate");
    }

    private static X509Certificate2 CarregarCertComChave()
        => CofreTestHelpers.CarregarPfx(CofreTestHelpers.GerarPfx());
}
