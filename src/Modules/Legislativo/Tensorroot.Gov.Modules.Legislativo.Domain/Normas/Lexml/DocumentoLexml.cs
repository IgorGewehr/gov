using System.Globalization;
using System.Xml.Linq;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Normas.Lexml;

/// <summary>
/// Gerador da representacao XML LexML-BR de uma <see cref="Norma"/> (export LOCAL — sem credencial; a
/// transmissao oficial e M10). Produz um documento aderente ao perfil LexML brasileiro: namespace
/// <c>http://www.lexml.gov.br/1.0</c>, elemento-raiz <c>LexML</c> com <c>Metadado</c> (Identificacao por
/// URN + epigrafe + ementa + datas) e <c>Norma</c> (parte inicial + corpo articulado quando houver).
/// <para>
/// Determinismo: a mesma norma + ente geram sempre o mesmo XML (URN canonica + ordenacao fixa), o que
/// permite hash/colacao e idempotencia no acervo de dados abertos. Sem numeros magicos: o tipo, a
/// jurisdicao e a autoridade vem do <see cref="IdentificacaoEnte"/> (parametro por tenant).
/// </para>
/// </summary>
public static class DocumentoLexml
{
    /// <summary>Namespace do perfil LexML-BR (versao 1.0).</summary>
    public const string NamespaceLexml = "http://www.lexml.gov.br/1.0";

    private static readonly XNamespace Ns = NamespaceLexml;

    /// <summary>
    /// Constroi o <see cref="XDocument"/> LexML da norma. Read-only sobre o agregado.
    /// </summary>
    /// <param name="norma">Norma a exportar.</param>
    /// <param name="ente">Identificacao do ente/autoridade (esfera, UF, municipio, autoridade).</param>
    /// <returns>Documento XML LexML.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="norma"/> ou <paramref name="ente"/> forem nulos.</exception>
    public static XDocument Construir(Norma norma, IdentificacaoEnte ente)
    {
        ArgumentNullException.ThrowIfNull(norma);
        ArgumentNullException.ThrowIfNull(ente);

        var urn = UrnLexml.Compor(ente, norma.Tipo, norma.DataPromulgacao, norma.Numero);
        var epigrafe = Epigrafe(norma);

        var metadado = new XElement(
            Ns + "Metadado",
            new XElement(
                Ns + "Identificacao",
                new XAttribute("URN", urn.Valor),
                new XElement(Ns + "Epigrafe", epigrafe),
                new XElement(Ns + "Ementa", norma.Ementa.Valor)),
            new XElement(
                Ns + "Documento",
                new XElement(Ns + "TipoDocumento", UrnLexml.MapearTipo(norma.Tipo)),
                new XElement(Ns + "Autoridade", ente.Autoridade),
                new XElement(Ns + "Localidade", ente.Local),
                new XElement(Ns + "Numero", norma.Numero.ToString(CultureInfo.InvariantCulture)),
                new XElement(Ns + "Ano", norma.Ano.ToString(CultureInfo.InvariantCulture)),
                new XElement(Ns + "DataPromulgacao", norma.DataPromulgacao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement(Ns + "SituacaoVigencia", norma.SituacaoVigencia.ToString()),
                norma.DataRevogacao is { } revogacao
                    ? new XElement(Ns + "DataRevogacao", revogacao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                    : null));

        var normaElemento = new XElement(
            Ns + "Norma",
            new XElement(
                Ns + "ParteInicial",
                new XElement(Ns + "Epigrafe", epigrafe),
                new XElement(Ns + "Ementa", norma.Ementa.Valor)),
            CorpoArticulado(norma));

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Ns + "LexML", metadado, normaElemento));
    }

    /// <summary>Serializa a norma como string XML LexML canonica (UTF-8, indentada).</summary>
    /// <param name="norma">Norma a exportar.</param>
    /// <param name="ente">Identificacao do ente/autoridade.</param>
    /// <returns>XML LexML como string.</returns>
    public static string Serializar(Norma norma, IdentificacaoEnte ente)
    {
        var documento = Construir(norma, ente);
        return documento.Declaration + Environment.NewLine + documento.ToString(SaveOptions.None);
    }

    // Epigrafe legivel: "LEI N. 1234, DE 10 DE MARCO DE 2025" — derivada do tipo/numero/ano.
    private static string Epigrafe(Norma norma)
    {
        var rotulo = norma.Tipo switch
        {
            TipoNorma.Lei => "LEI",
            TipoNorma.LeiComplementar => "LEI COMPLEMENTAR",
            TipoNorma.DecretoLegislativo => "DECRETO LEGISLATIVO",
            TipoNorma.Resolucao => "RESOLUCAO",
            TipoNorma.EmendaLOM => "EMENDA A LEI ORGANICA",
            TipoNorma.LeiOrganica => "LEI ORGANICA",
            _ => norma.Tipo.ToString().ToUpperInvariant(),
        };

        return $"{rotulo} N. {norma.Numero.ToString(CultureInfo.InvariantCulture)}/{norma.Ano.ToString(CultureInfo.InvariantCulture)}";
    }

    // Corpo articulado: quando ha texto, embrulha-o num <Articulacao> com um <Caput> literal (sem motor
    // de parsing de artigos — fora do escopo W9.5; o texto livre e preservado integralmente).
    private static XElement CorpoArticulado(Norma norma)
        => string.IsNullOrWhiteSpace(norma.TextoArticulado)
            ? new XElement(Ns + "Articulacao")
            : new XElement(Ns + "Articulacao", new XElement(Ns + "TextoIntegral", norma.TextoArticulado));
}
