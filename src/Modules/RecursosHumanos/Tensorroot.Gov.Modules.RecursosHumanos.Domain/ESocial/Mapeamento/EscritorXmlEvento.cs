using System.Globalization;
using System.Text;
using System.Xml;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;

/// <summary>
/// Helper de escrita de XML de evento eSocial com a DISCIPLINA de namespace exigida pelo MOS v1.15
/// (ESOCIAL-SPEC §2.3): UMA unica declaracao de namespace no elemento raiz <c>eSocial</c>, sem prefixos
/// fora do padrao, UTF-8 sem BOM, sem identacao (assinatura C14N inclusiva). O elemento do evento carrega
/// o atributo <c>Id</c> (identificador de negocio, NAO alvo da assinatura). // TODO(validar-oficial): o XML
/// final DEVE ser gerado/validado contra o XSD da versao travada (S-1.3) antes de assinar (ESOCIAL-SPEC §2.6);
/// este escritor produz a ESTRUTURA fiel ao conceito do leiaute, nao substitui a validacao XSD.
/// </summary>
public static class EscritorXmlEvento
{
    /// <summary>Prefixo do namespace de cada evento. // TODO(validar-oficial: versao exata vx_x_x do XSD).</summary>
    public const string NamespaceEventoBase = "http://www.esocial.gov.br/schema/evt";

    private static readonly UTF8Encoding Utf8SemBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Escreve um documento de evento: raiz <c>eSocial</c> (com o namespace do evento), elemento do evento
    /// com o atributo <c>Id</c> e o corpo montado por <paramref name="corpo"/>.
    /// </summary>
    /// <param name="nomeEvento">Nome do elemento do evento (ex.: <c>evtInfoEmpregador</c>).</param>
    /// <param name="namespaceEvento">Namespace completo do evento (raiz unica).</param>
    /// <param name="idEvento">Valor do atributo <c>Id</c> do evento.</param>
    /// <param name="corpo">Acao que escreve o corpo do evento (filhos do elemento do evento).</param>
    /// <returns>Bytes UTF-8 do XML.</returns>
    /// <exception cref="ArgumentException">Se nome/namespace/id forem vazios.</exception>
    /// <exception cref="ArgumentNullException">Se <paramref name="corpo"/> for nulo.</exception>
    public static byte[] Escrever(string nomeEvento, string namespaceEvento, string idEvento, Action<XmlWriter> corpo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeEvento);
        ArgumentException.ThrowIfNullOrWhiteSpace(namespaceEvento);
        ArgumentException.ThrowIfNullOrWhiteSpace(idEvento);
        ArgumentNullException.ThrowIfNull(corpo);

        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = Utf8SemBom,
            Indent = false,
            OmitXmlDeclaration = false,
            CloseOutput = false,
        };

        using (var writer = XmlWriter.Create(ms, settings))
        {
            // Raiz UNICA com a unica declaracao de namespace (default), conforme MOS §623-638.
            writer.WriteStartElement("eSocial", namespaceEvento);
            writer.WriteStartElement(nomeEvento);
            writer.WriteAttributeString("Id", idEvento);
            corpo(writer);
            writer.WriteEndElement(); // nomeEvento
            writer.WriteEndElement(); // eSocial
            writer.Flush();
        }

        return ms.ToArray();
    }

    /// <summary>Escreve um elemento simples (texto) se o valor nao for nulo/vazio.</summary>
    /// <param name="writer">Escritor XML.</param>
    /// <param name="nome">Nome do elemento.</param>
    /// <param name="valor">Valor textual (ignorado se vazio).</param>
    public static void EscreverSeTiver(this XmlWriter writer, string nome, string? valor)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!string.IsNullOrWhiteSpace(valor))
        {
            writer.WriteElementString(nome, valor);
        }
    }

    /// <summary>Formata uma data no padrao do leiaute (<c>AAAA-MM-DD</c>).</summary>
    /// <param name="data">Data.</param>
    /// <returns>Texto <c>AAAA-MM-DD</c>.</returns>
    public static string Data(DateOnly data) => data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Formata um valor monetario/decimal no padrao do leiaute (ponto decimal, 2 casas).</summary>
    /// <param name="valor">Valor.</param>
    /// <returns>Texto invariante com 2 casas.</returns>
    public static string Valor(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);
}
