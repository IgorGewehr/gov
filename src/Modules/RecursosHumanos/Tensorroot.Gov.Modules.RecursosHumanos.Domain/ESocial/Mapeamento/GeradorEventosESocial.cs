using System.Xml;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;

/// <summary>
/// ACL de mapeamento DOMINIO -> LEIAUTE eSocial (servico de dominio PURO, sem I/O). Traduz nossos
/// agregados/insumos em XML de evento ESTRUTURALMENTE FIEL ao leiaute S-1.3 (S-1000/1005/1010/2200/2299/
/// 1200/1202/1210/1299). Os tipos do leiaute NAO vazam para o dominio (recebe <c>InsumoSxxxx</c>).
/// <para>
/// REGRA CRITICA (CLAUDE.md §16 / ESOCIAL-SPEC): a estrutura segue FIELMENTE o conceito do leiaute, mas
/// os campos/XSD EXATOS da versao travada estao marcados <c>// TODO(validar-oficial)</c> e o XML DEVE ser
/// validado contra o XSD oficial antes da assinatura/transmissao (ESOCIAL-SPEC §2.6). Nenhuma
/// alIquota/tabela e hardcoded — os codigos de incidencia/categoria chegam pelos insumos.
/// </para>
/// </summary>
public static class GeradorEventosESocial
{
    private static string Ns(string sufixo) => $"{EscritorXmlEvento.NamespaceEventoBase}/{sufixo}";

    /// <summary>Gera o XML do S-1000 (Informacoes do Empregador/Orgao Publico).</summary>
    /// <param name="insumo">Dados do empregador.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS1000(InsumoS1000 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        // // TODO(validar-oficial): namespace/versao exatos do XSD S-1000 (evtInfoEmpregador).
        return EscritorXmlEvento.Escrever("evtInfoEmpregador", Ns("infoEmpregador/v_S_01_03_00"), idEvento, w =>
        {
            w.WriteStartElement("ideEvento");
            w.WriteElementString("tpAmb", ((int)insumo.TpInsc == 0 ? 1 : 1).ToString(System.Globalization.CultureInfo.InvariantCulture)); // // TODO(validar-oficial): tpAmb vem do ambiente do EventoESocial; placeholder estrutural.
            w.WriteElementString("procEmi", "1"); // // TODO(validar-oficial: dominio procEmi/verProc).
            w.WriteEndElement();

            w.WriteStartElement("ideEmpregador");
            w.WriteElementString("tpInsc", ((int)insumo.TpInsc).ToString(System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("nrInsc", insumo.NrInsc);
            w.WriteEndElement();

            w.WriteStartElement("infoEmpregador");
            w.WriteStartElement("inclusao");
            w.WriteStartElement("idePeriodo");
            w.WriteElementString("iniValid", insumo.InicioValidade);
            w.WriteEndElement(); // idePeriodo
            w.WriteStartElement("infoCadastro");
            w.WriteElementString("classTrib", insumo.ClassTrib); // // TODO(validar-oficial: Tabela 08).
            // infoEFR — obrigatorio p/ ente publico (ESOCIAL-SPEC §1.1).
            if (!string.IsNullOrWhiteSpace(insumo.NrInscEfr))
            {
                w.WriteStartElement("infoEFR");
                w.WriteElementString("ideEFR", "S");
                w.WriteElementString("nrInsc", insumo.NrInscEfr);
                w.WriteEndElement();
            }

            w.WriteEndElement(); // infoCadastro
            w.WriteEndElement(); // inclusao
            w.WriteEndElement(); // infoEmpregador
        });
    }

    /// <summary>Gera o XML do S-1005 (Tabela de Estabelecimentos/Unidades).</summary>
    /// <param name="insumo">Dados do estabelecimento.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS1005(InsumoS1005 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        return EscritorXmlEvento.Escrever("evtTabEstab", Ns("tabEstab/v_S_01_03_00"), idEvento, w =>
        {
            EscreverIdeEventoTabela(w);
            w.WriteStartElement("infoEstab");
            w.WriteStartElement("inclusao");
            w.WriteStartElement("ideEstab");
            w.WriteElementString("tpInsc", ((int)insumo.TpInsc).ToString(System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("nrInsc", insumo.NrInsc);
            w.WriteElementString("iniValid", insumo.InicioValidade);
            w.WriteEndElement(); // ideEstab
            w.WriteStartElement("dadosEstab");
            w.WriteElementString("cnaePrep", insumo.CnaePrep);
            if (insumo.AliqGilrat is { } aliq)
            {
                // // TODO(validar-oficial): aplicabilidade de aliqGilrat/FAP a orgao publico RPPS.
                w.WriteStartElement("aliqGilrat");
                w.WriteElementString("aliqRat", EscritorXmlEvento.Valor(aliq));
                w.WriteEndElement();
            }

            w.WriteEndElement(); // dadosEstab
            w.WriteEndElement(); // inclusao
            w.WriteEndElement(); // infoEstab
        });
    }

    /// <summary>Gera o XML do S-1010 (Tabela de Rubricas) — mapeado da nossa RubricaFolha.</summary>
    /// <param name="insumo">Dados da rubrica (com codigos de incidencia).</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS1010(InsumoS1010 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        return EscritorXmlEvento.Escrever("evtTabRubrica", Ns("tabRubrica/v_S_01_03_00"), idEvento, w =>
        {
            EscreverIdeEventoTabela(w);
            w.WriteStartElement("infoRubrica");
            w.WriteStartElement("inclusao");
            w.WriteStartElement("ideRubrica");
            w.WriteElementString("codRubr", insumo.CodRubr);
            w.WriteElementString("ideTabRubr", insumo.IdeTabRubr);
            w.WriteElementString("iniValid", insumo.InicioValidade);
            w.WriteEndElement(); // ideRubrica
            w.WriteStartElement("dadosRubrica");
            w.WriteElementString("dscRubr", insumo.DscRubr);
            w.WriteElementString("natRubr", insumo.NatRubr); // // TODO(validar-oficial: Tabela 03).
            w.WriteElementString("tpRubr", insumo.TpRubr.ToString(System.Globalization.CultureInfo.InvariantCulture));
            w.WriteElementString("codIncCP", insumo.CodIncCp); // // TODO(validar-oficial: Tabela 20).
            w.WriteElementString("codIncIRRF", insumo.CodIncIrrf); // // TODO(validar-oficial: Tabela 21).
            w.WriteElementString("codIncFGTS", insumo.CodIncFgts); // // TODO(validar-oficial: Tabela 23).
            w.WriteEndElement(); // dadosRubrica
            w.WriteEndElement(); // inclusao
            w.WriteEndElement(); // infoRubrica
        });
    }

    /// <summary>Gera o XML do S-2200 (Admissao/Cadastramento Inicial do Vinculo).</summary>
    /// <param name="insumo">Snapshot do servidor admitido.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS2200(InsumoS2200 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        return EscritorXmlEvento.Escrever("evtAdmissao", Ns("evtAdmissao/v_S_01_03_00"), idEvento, w =>
        {
            EscreverIdeEventoNaoPeriodico(w);
            w.WriteStartElement("trabalhador");
            w.WriteElementString("cpfTrab", insumo.CpfTrab);
            w.WriteElementString("nmTrab", insumo.NomeTrab);
            w.WriteElementString("dtNascto", EscritorXmlEvento.Data(insumo.DataNascimento)); // // TODO(validar-oficial: grupo nascimento).
            w.WriteEndElement(); // trabalhador

            w.WriteStartElement("vinculo");
            w.WriteElementString("matricula", insumo.Matricula);
            w.WriteElementString("tpRegPrev", insumo.TpRegPrev.ToString(System.Globalization.CultureInfo.InvariantCulture)); // 1=RGPS,2=RPPS,3=Ext,4=SPSMFA.
            w.WriteStartElement("infoContrato");
            w.WriteElementString("codCargo", insumo.CodCargo);
            w.WriteElementString("codCateg", insumo.CodCateg); // // TODO(validar-oficial: Tabela 01).
            w.WriteStartElement("remuneracao");
            w.WriteElementString("vrSalFx", EscritorXmlEvento.Valor(insumo.VrSalFx));
            w.WriteEndElement(); // remuneracao
            w.WriteStartElement("infoRegimeTrab");
            w.WriteStartElement("infoEstatutario");
            w.WriteElementString("dtNomeacao", EscritorXmlEvento.Data(insumo.DataAdmissao)); // // TODO(validar-oficial: tpProv/dtPosse/dtExercicio).
            w.WriteEndElement(); // infoEstatutario
            w.WriteEndElement(); // infoRegimeTrab
            w.WriteEndElement(); // infoContrato
            w.WriteEndElement(); // vinculo
        });
    }

    /// <summary>Gera o XML do S-2299 (Desligamento).</summary>
    /// <param name="insumo">Snapshot do desligamento.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS2299(InsumoS2299 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        return EscritorXmlEvento.Escrever("evtDeslig", Ns("evtDeslig/v_S_01_03_00"), idEvento, w =>
        {
            EscreverIdeEventoNaoPeriodico(w);
            w.WriteStartElement("ideVinculo");
            w.WriteElementString("cpfTrab", insumo.CpfTrab);
            w.WriteElementString("matricula", insumo.Matricula);
            w.WriteEndElement(); // ideVinculo
            w.WriteStartElement("infoDeslig");
            w.WriteElementString("mtvDeslig", insumo.MtvDeslig); // // TODO(validar-oficial: Tabela 19).
            w.WriteElementString("dtDeslig", EscritorXmlEvento.Data(insumo.DataDesligamento));
            w.WriteEndElement(); // infoDeslig
        });
    }

    /// <summary>Gera o XML do S-1200 (Remuneracao RGPS) ou S-1202 (Remuneracao RPPS), conforme <paramref name="rpps"/>.</summary>
    /// <param name="insumo">Snapshot de remuneracao do servidor.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <param name="rpps">Verdadeiro para S-1202 (RPPS); falso para S-1200 (RGPS).</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS1200(InsumoS1200 insumo, string idEvento, bool rpps)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        var nomeEvento = rpps ? "evtRmnRPPS" : "evtRemun";
        var ns = rpps ? Ns("evtRmnRPPS/v_S_01_03_00") : Ns("evtRemun/v_S_01_03_00");
        return EscritorXmlEvento.Escrever(nomeEvento, ns, idEvento, w =>
        {
            w.WriteStartElement("ideEvento");
            w.WriteElementString("indRetif", "1"); // // TODO(validar-oficial: 1=original, 2=retificacao).
            w.WriteElementString("perApur", insumo.PerApur);
            EscreverProcEmiVerProc(w);
            w.WriteEndElement(); // ideEvento

            w.WriteStartElement("ideTrabalhador");
            w.WriteElementString("cpfTrab", insumo.CpfTrab);
            w.WriteEndElement(); // ideTrabalhador

            // dmDev: demonstrativo de valores devidos (1 por matricula/categoria). // TODO(validar-oficial).
            w.WriteStartElement("dmDev");
            w.WriteElementString("ideDmDev", insumo.Matricula);
            w.WriteElementString("codCateg", insumo.CodCateg);
            w.WriteStartElement("infoPerApur");
            w.WriteStartElement("ideEstabLot");
            // detVerbas: somado por rubrica/incidencia — a folha E a fonte de verdade (ESOCIAL-SPEC §1.6).
            foreach (var v in insumo.Verbas)
            {
                w.WriteStartElement("detVerbas");
                w.WriteElementString("codRubr", v.CodRubr);
                w.WriteElementString("ideTabRubr", v.IdeTabRubr);
                w.WriteElementString("qtdRubr", EscritorXmlEvento.Valor(v.QtdRubr));
                w.WriteElementString("vrRubr", EscritorXmlEvento.Valor(v.VrRubr));
                w.WriteElementString("indApurIR", v.IndApurIr.ToString(System.Globalization.CultureInfo.InvariantCulture));
                w.WriteEndElement(); // detVerbas
            }

            w.WriteEndElement(); // ideEstabLot
            w.WriteEndElement(); // infoPerApur
            w.WriteEndElement(); // dmDev
        });
    }

    /// <summary>Gera o XML do S-1210 (Pagamentos de Rendimentos do Trabalho).</summary>
    /// <param name="insumo">Snapshot do pagamento.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS1210(InsumoS1210 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        return EscritorXmlEvento.Escrever("evtPgtos", Ns("evtPgtos/v_S_01_03_00"), idEvento, w =>
        {
            w.WriteStartElement("ideEvento");
            w.WriteElementString("indApuracao", "1"); // // TODO(validar-oficial: 1=mensal.
            w.WriteElementString("perApur", insumo.PerRef);
            EscreverProcEmiVerProc(w);
            w.WriteEndElement(); // ideEvento

            w.WriteStartElement("ideBenef");
            w.WriteElementString("cpfBenef", insumo.CpfBenef);
            w.WriteStartElement("infoPgto");
            w.WriteElementString("dtPgto", EscritorXmlEvento.Data(insumo.DataPagamento));
            w.WriteElementString("tpPgto", "1"); // // TODO(validar-oficial: dominio tpPgto.
            w.WriteElementString("perRef", insumo.PerRef);
            w.WriteElementString("vrLiq", EscritorXmlEvento.Valor(insumo.VrLiquido));
            w.WriteEndElement(); // infoPgto
            w.WriteEndElement(); // ideBenef
        });
    }

    /// <summary>Gera o XML do S-1299 (Fechamento dos Eventos Periodicos).</summary>
    /// <param name="insumo">Snapshot do fechamento.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS1299(InsumoS1299 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        return EscritorXmlEvento.Escrever("evtFechaEvPer", Ns("evtFechaEvPer/v_S_01_03_00"), idEvento, w =>
        {
            w.WriteStartElement("ideEvento");
            w.WriteElementString("indApuracao", "1");
            w.WriteElementString("perApur", insumo.PerApur);
            EscreverProcEmiVerProc(w);
            w.WriteEndElement(); // ideEvento

            w.WriteStartElement("infoFech");
            w.WriteElementString("evtRemun", insumo.HouveRemun ? "S" : "N");
            w.WriteElementString("evtPgtos", insumo.HouvePgto ? "S" : "N");
            // // TODO(validar-oficial): demais flags (evtAqProd/evtComProd/evtContratAvNP/compSemMovto).
            w.WriteEndElement(); // infoFech
        });
    }

    private static void EscreverIdeEventoTabela(XmlWriter w)
    {
        // // TODO(validar-oficial): grupo ideEvento de eventos de tabela (tpAmb/procEmi/verProc).
        w.WriteStartElement("ideEvento");
        EscreverProcEmiVerProc(w);
        w.WriteEndElement();
    }

    private static void EscreverIdeEventoNaoPeriodico(XmlWriter w)
    {
        w.WriteStartElement("ideEvento");
        w.WriteElementString("indRetif", "1");
        EscreverProcEmiVerProc(w);
        w.WriteEndElement();
    }

    private static void EscreverProcEmiVerProc(XmlWriter w)
    {
        // procEmi=1 (aplicativo do empregador) / verProc = versao do nosso processo emissor.
        // // TODO(validar-oficial): dominio de procEmi e formato de verProc no XSD.
        w.WriteElementString("procEmi", "1");
        w.WriteElementString("verProc", "Tensorroot.Gov-eSocial");
    }
}
