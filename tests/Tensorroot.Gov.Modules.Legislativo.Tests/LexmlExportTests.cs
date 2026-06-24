using System.Xml.Linq;
using FluentAssertions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas.Lexml;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// W9.5 — URN + export XML LexML-BR de uma norma. Prova a composicao canonica da URN
/// (urn:lex:br;rs;municipio:autoridade:tipo:data;numero), o determinismo do XML (mesma norma ->
/// mesmo documento), a aderencia ao perfil LexML (namespace 1.0, Identificacao por URN) e a
/// normalizacao LexML do municipio/UF (sem acento, minusculo, com pontos).
/// </summary>
public sealed class LexmlExportTests
{
    private static readonly Guid TenantCamara = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static IdentificacaoEnte EnteMaximiliano()
        => IdentificacaoEnte.De("RS", "Maximiliano de Almeida", "Camara Municipal");

    private static Norma NormaLei1234()
        => Norma.Promulgar(
            TenantCamara,
            TipoNorma.Lei,
            1234,
            2025,
            Ementa.De("Dispoe sobre o calendario de eventos do municipio."),
            new DateOnly(2025, 3, 10),
            "Art. 1. Fica instituido o calendario oficial de eventos.");

    [Fact] // URN LexML canonica: urn:lex:[local]:[autoridade]:[tipo]:[data];[numero]
    public void Compor_urn_gera_string_canonica_lexml()
    {
        var ente = EnteMaximiliano();

        var urn = UrnLexml.Compor(ente, TipoNorma.Lei, new DateOnly(2025, 3, 10), 1234);

        urn.Valor.Should().Be("urn:lex:br;rs;maximiliano.de.almeida:camara.municipal:lei:2025-03-10;1234");
    }

    [Fact] // O ente normaliza a grafia LexML (sem acento, minusculo, espacos -> ponto).
    public void Ente_normaliza_municipio_para_grafia_lexml()
    {
        var ente = IdentificacaoEnte.De("RS", "Sao Joao do Polesine", "Camara Municipal");

        ente.Local.Should().Be("br;rs;sao.joao.do.polesine");
        ente.Autoridade.Should().Be("camara.municipal");
    }

    [Fact] // Tipos diferentes mapeiam para o vocabulario LexML correto.
    public void MapearTipo_usa_vocabulario_lexml()
    {
        UrnLexml.MapearTipo(TipoNorma.Lei).Should().Be("lei");
        UrnLexml.MapearTipo(TipoNorma.LeiComplementar).Should().Be("lei.complementar");
        UrnLexml.MapearTipo(TipoNorma.Resolucao).Should().Be("resolucao");
    }

    [Fact] // Export XML: namespace LexML 1.0 + Identificacao com a URN da norma.
    public void Serializar_gera_xml_lexml_com_urn_no_metadado()
    {
        var ente = EnteMaximiliano();
        var norma = NormaLei1234();

        var documento = DocumentoLexml.Construir(norma, ente);

        XNamespace ns = DocumentoLexml.NamespaceLexml;
        documento.Root!.Name.Should().Be(ns + "LexML");

        var identificacao = documento.Root!
            .Element(ns + "Metadado")!
            .Element(ns + "Identificacao")!;

        identificacao.Attribute("URN")!.Value
            .Should().Be("urn:lex:br;rs;maximiliano.de.almeida:camara.municipal:lei:2025-03-10;1234");

        // O texto articulado e preservado integralmente no corpo.
        documento.ToString().Should().Contain("calendario oficial de eventos");
    }

    [Fact] // Determinismo: mesma norma + ente -> XML identico (permite hash/colacao/idempotencia).
    public void Serializar_e_deterministico()
    {
        var ente = EnteMaximiliano();

        var xml1 = DocumentoLexml.Serializar(NormaLei1234(), ente);
        var xml2 = DocumentoLexml.Serializar(NormaLei1234(), ente);

        xml1.Should().Be(xml2);
    }

    [Fact] // Numero nao-positivo e rejeitado na composicao da URN.
    public void Compor_rejeita_numero_nao_positivo()
    {
        var ente = EnteMaximiliano();

        var acao = () => UrnLexml.Compor(ente, TipoNorma.Lei, new DateOnly(2025, 3, 10), 0);

        acao.Should().Throw<ArgumentException>();
    }
}
