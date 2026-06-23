using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura dos GERADORES (ACL dominio -> leiaute): cada evento produz XML BEM-FORMADO, com uma unica
/// declaracao de namespace na raiz <c>eSocial</c> (disciplina do MOS), o atributo <c>Id</c> no elemento
/// do evento e os campos-chave do leiaute. Tambem cobre o roteador S-1200 (RGPS) x S-1202 (RPPS).
/// </summary>
public sealed class ESocialGeradorTests
{
    private const string Id = "ID11222333000181202606231200000001";

    private static XDocument Parse(byte[] xml)
    {
        // Bem-formado (parse falha se nao for) + UTF-8 (gerador escreve UTF-8 sem BOM).
        var texto = Encoding.UTF8.GetString(xml);
        return XDocument.Parse(texto);
    }

    private static XElement Raiz(XDocument doc) => doc.Root!;

    [Fact] // S-1000: raiz eSocial, evtInfoEmpregador com Id, ideEmpregador, EFR quando informado.
    public void S1000_gera_estrutura_fiel()
    {
        var insumo = new InsumoS1000(TipoInscricao.Cnpj, "11222333000181", "MUNICIPIO X", "01", "99888777000166", "2026-06");

        var xml = GeradorEventosESocial.GerarS1000(insumo, Id);
        var doc = Parse(xml);
        var raiz = Raiz(doc);

        raiz.Name.LocalName.Should().Be("eSocial");
        var evt = raiz.Elements().Single();
        evt.Name.LocalName.Should().Be("evtInfoEmpregador");
        evt.Attribute("Id")!.Value.Should().Be(Id);
        doc.Descendants().Any(e => e.Name.LocalName == "nrInsc" && e.Value == "11222333000181").Should().BeTrue();
        // EFR obrigatorio para ente publico presente.
        doc.Descendants().Any(e => e.Name.LocalName == "infoEFR").Should().BeTrue();
    }

    [Fact] // S-1010: codigos de incidencia da rubrica no XML (CONFIRMADO: informativa => codIncCP=00, codIncIRRF=9).
    public void S1010_mapeia_incidencias_da_rubrica()
    {
        var insumo = new InsumoS1010("VENC", "RUBRICAS", "Vencimento", "1000", 1, "11", "11", "00", "2026-06");

        var xml = GeradorEventosESocial.GerarS1010(insumo, Id);
        var doc = Parse(xml);

        doc.Descendants().Single(e => e.Name.LocalName == "codRubr").Value.Should().Be("VENC");
        doc.Descendants().Single(e => e.Name.LocalName == "tpRubr").Value.Should().Be("1");
        doc.Descendants().Single(e => e.Name.LocalName == "codIncCP").Value.Should().Be("11");
        doc.Descendants().Single(e => e.Name.LocalName == "codIncIRRF").Value.Should().Be("11");
    }

    [Fact] // S-2200: CPF, matricula, tpRegPrev e codCateg do servidor.
    public void S2200_gera_admissao()
    {
        var insumo = new InsumoS2200(
            "12345678901", "Fulano", new DateOnly(1990, 1, 1), "MAT1",
            new DateOnly(2026, 1, 5), "301", RoteadorRemuneracao.TpRegPrevRpps, "CARGO1", 5000m);

        var xml = GeradorEventosESocial.GerarS2200(insumo, Id);
        var doc = Parse(xml);

        Raiz(doc).Elements().Single().Name.LocalName.Should().Be("evtAdmissao");
        doc.Descendants().Single(e => e.Name.LocalName == "cpfTrab").Value.Should().Be("12345678901");
        doc.Descendants().Single(e => e.Name.LocalName == "tpRegPrev").Value.Should().Be("2");
        doc.Descendants().Single(e => e.Name.LocalName == "codCateg").Value.Should().Be("301");
    }

    [Fact] // S-1200 (RGPS): nome do evento evtRemun e detVerbas somado por rubrica.
    public void S1200_rgps_gera_remuneracao_com_detVerbas()
    {
        var verbas = new List<ItemVerba>
        {
            new("VENC", "RUBRICAS", 1m, 5000m, 0),
            new("INSS", "RUBRICAS", 1m, 550m, 0),
        };
        var insumo = new InsumoS1200("12345678901", "MAT1", "101", "2026-06", verbas);

        var xml = GeradorEventosESocial.GerarS1200(insumo, Id, rpps: false);
        var doc = Parse(xml);

        Raiz(doc).Elements().Single().Name.LocalName.Should().Be("evtRemun");
        doc.Descendants().Count(e => e.Name.LocalName == "detVerbas").Should().Be(2);
        doc.Descendants().Single(e => e.Name.LocalName == "perApur").Value.Should().Be("2026-06");
    }

    [Fact] // S-1202 (RPPS): roteado para evtRmnRPPS.
    public void S1202_rpps_gera_evento_rpps()
    {
        var insumo = new InsumoS1200("12345678901", "MAT1", "301", "2026-06", [new("VENC", "RUBRICAS", 1m, 8000m, 0)]);

        var xml = GeradorEventosESocial.GerarS1200(insumo, Id, rpps: true);
        var doc = Parse(xml);

        Raiz(doc).Elements().Single().Name.LocalName.Should().Be("evtRmnRPPS");
    }

    [Fact] // S-1210: dtPgto e vrLiq do pagamento.
    public void S1210_gera_pagamento()
    {
        var insumo = new InsumoS1210("12345678901", "2026-06", new DateOnly(2026, 7, 5), 4450m);

        var xml = GeradorEventosESocial.GerarS1210(insumo, Id);
        var doc = Parse(xml);

        Raiz(doc).Elements().Single().Name.LocalName.Should().Be("evtPgtos");
        doc.Descendants().Single(e => e.Name.LocalName == "vrLiq").Value.Should().Be("4450.00");
        doc.Descendants().Single(e => e.Name.LocalName == "dtPgto").Value.Should().Be("2026-07-05");
    }

    [Fact] // S-1299: flags evtRemun/evtPgtos do fechamento.
    public void S1299_gera_fechamento_com_flags()
    {
        var insumo = new InsumoS1299("2026-06", HouveRemun: true, HouvePgto: false);

        var xml = GeradorEventosESocial.GerarS1299(insumo, Id);
        var doc = Parse(xml);

        Raiz(doc).Elements().Single().Name.LocalName.Should().Be("evtFechaEvPer");
        doc.Descendants().Single(e => e.Name.LocalName == "evtRemun").Value.Should().Be("S");
        doc.Descendants().Single(e => e.Name.LocalName == "evtPgtos").Value.Should().Be("N");
    }

    [Theory] // Roteador: RPPS -> S-1202/tpRegPrev=2; RGPS -> S-1200/tpRegPrev=1.
    [InlineData(RegimePrevidenciario.Rpps, true, 2)]
    [InlineData(RegimePrevidenciario.Rgps, false, 1)]
    public void Roteador_remuneracao_por_regime(RegimePrevidenciario regime, bool esperaRpps, int tpRegPrev)
    {
        RoteadorRemuneracao.EhRpps(regime).Should().Be(esperaRpps);
        RoteadorRemuneracao.DerivarTpRegPrev(regime).Should().Be(tpRegPrev);
        RoteadorRemuneracao.TipoRemuneracao(regime).Should().Be(
            esperaRpps ? TipoEventoESocial.S1202RemuneracaoRpps : TipoEventoESocial.S1200Remuneracao);
    }
}
