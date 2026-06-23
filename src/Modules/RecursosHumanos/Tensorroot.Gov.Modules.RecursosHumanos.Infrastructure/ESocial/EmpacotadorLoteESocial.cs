using System.Text;
using System.Xml;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.ESocial;

/// <summary>
/// Monta o XML do envelope de envio (<c>envioLoteEventos</c>) do schema <c>EnvioLoteEventos</c>
/// (ESOCIAL-SPEC §2.3): raiz <c>eSocial</c> com <c>ideEmpregador</c>/<c>ideTransmissor</c> e os eventos
/// JA ASSINADOS embutidos sob <c>&lt;evento Id="..."&gt;</c>. // TODO(validar-oficial): namespace/versao
/// exatos do XSD do lote (vx_x_x) e estrutura do ideTransmissor. Este empacotador produz a ESTRUTURA
/// fiel; a TRANSMISSAO real (SOAP 1.2/mTLS) e responsabilidade do gateway real.
/// </summary>
public static class EmpacotadorLoteESocial
{
    /// <summary>Namespace do lote de envio. // TODO(validar-oficial: versao vx_x_x exata do XSD).</summary>
    public const string NamespaceLote = "http://www.esocial.gov.br/schema/lote/eventos/envio/v_S_01_03_00";

    private static readonly UTF8Encoding Utf8SemBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Monta os bytes UTF-8 do envelope de envio do lote.</summary>
    /// <param name="lote">Lote (ja validado: &lt;=50 eventos e &lt;=5 MB).</param>
    /// <returns>XML (UTF-8) do <c>envioLoteEventos</c>.</returns>
    public static byte[] Empacotar(LoteEventosESocial lote)
    {
        ArgumentNullException.ThrowIfNull(lote);

        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = Utf8SemBom, Indent = false, OmitXmlDeclaration = false, CloseOutput = false };
        using (var w = XmlWriter.Create(ms, settings))
        {
            w.WriteStartElement("eSocial", NamespaceLote);
            w.WriteStartElement("envioLoteEventos");
            w.WriteAttributeString("grupo", ((int)lote.Ambiente).ToString(System.Globalization.CultureInfo.InvariantCulture)); // // TODO(validar-oficial: dominio do atributo grupo).

            w.WriteStartElement("ideEmpregador");
            w.WriteElementString("tpInsc", ((int)lote.TpInscEmpregador).ToString(System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("nrInsc", lote.NrInscEmpregador);
            w.WriteEndElement(); // ideEmpregador

            w.WriteStartElement("ideTransmissor");
            w.WriteElementString("tpInsc", ((int)lote.TpInscEmpregador).ToString(System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("nrInsc", lote.NrInscEmpregador); // transmissor = empregador (mesmo A1). // TODO(validar-oficial).
            w.WriteEndElement(); // ideTransmissor

            w.WriteStartElement("eventos");
            foreach (var item in lote.Itens)
            {
                w.WriteStartElement("evento");
                w.WriteAttributeString("Id", item.IdEvento);
                // Embute o XML do evento JA ASSINADO (sem reabrir/recanonicalizar — preserva a assinatura).
                EscreverFragmentoXml(w, item.XmlAssinado);
                w.WriteEndElement(); // evento
            }

            w.WriteEndElement(); // eventos
            w.WriteEndElement(); // envioLoteEventos
            w.WriteEndElement(); // eSocial
            w.Flush();
        }

        return ms.ToArray();
    }

    private static void EscreverFragmentoXml(XmlWriter destino, byte[] xmlAssinado)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        using var leitor = new MemoryStream(xmlAssinado);
        doc.Load(leitor);
        // Escreve o elemento raiz do evento assinado como filho de <evento>, preservando a assinatura.
        doc.DocumentElement?.WriteTo(destino);
    }
}
