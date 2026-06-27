using System.Globalization;
using System.Xml;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;

/// <summary>
/// ACL de mapeamento DOMINIO -> LEIAUTE para os eventos de SST (Saude e Seguranca do Trabalho) do eSocial:
/// S-2210 (CAT), S-2220 (Monitoramento da Saude/ASO) e S-2240 (Condicoes Ambientais/Agentes Nocivos).
/// Servico de dominio PURO (sem I/O). Produz XML ESTRUTURALMENTE FIEL ao leiaute S-1.3 (MOS); os campos/XSD
/// EXATOS da versao travada estao marcados <c>// TODO(validar-oficial)</c> e o XML DEVE ser validado contra
/// o XSD oficial antes da assinatura/transmissao (ESOCIAL-SPEC §2.6). Nenhuma tabela/codigo e hardcoded —
/// os dominios (Tabelas 13/23/25/26/27) chegam pelos insumos.
/// </summary>
public static class GeradorEventosSst
{
    private static string Ns(string sufixo) => $"{EscritorXmlEvento.NamespaceEventoBase}/{sufixo}";

    /// <summary>Gera o XML do S-2210 (Comunicacao de Acidente de Trabalho — CAT).</summary>
    /// <param name="insumo">Snapshot da CAT.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS2210(InsumoS2210 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        return EscritorXmlEvento.Escrever("evtCAT", Ns("evtCAT/v_S_01_03_00"), idEvento, w =>
        {
            EscreverIdeEventoNaoPeriodico(w);
            EscreverIdeEmpregador(w, insumo.Empregador);
            EscreverIdeVinculo(w, insumo.Vinculo);

            w.WriteStartElement("cat");
            w.WriteElementString("dtAcid", EscritorXmlEvento.Data(insumo.DataAcidente));
            w.WriteElementString("tpAcid", insumo.TpAcid.ToString(CultureInfo.InvariantCulture)); // // TODO(validar-oficial: Tabela 25).
            w.WriteElementString("hrAcid", insumo.HoraAcidente); // // TODO(validar-oficial: formato HHMM).
            w.WriteElementString("tpCat", insumo.TpCat.ToString(CultureInfo.InvariantCulture)); // 1=inicial,2=reabertura,3=obito.
            w.WriteElementString("indCatObito", insumo.IndCatObito);
            if (insumo.DataObito is { } dtObito)
            {
                w.WriteElementString("dtObito", EscritorXmlEvento.Data(dtObito));
            }

            w.WriteElementString("dscLesao", insumo.DescricaoSituacao); // // TODO(validar-oficial: dscLesao x situacaoGeradora).
            w.EscreverSeTiver("codCID", insumo.Cid); // // TODO(validar-oficial: Tabela 13/CID-10).
            w.EscreverSeTiver("lateralidade", insumo.ParteCorpoAtingida); // // TODO(validar-oficial: parteAtingida/Tabela 13).
            w.EscreverSeTiver("agenteCausador", insumo.AgenteCausador); // // TODO(validar-oficial: Tabela 13).

            // Reabertura/obito referenciam a CAT inicial (nrRecCatOrig). // TODO(validar-oficial: nome/posicao).
            w.EscreverSeTiver("nrRecCatOrig", insumo.NrRecCatOrig);
            w.WriteEndElement(); // cat
        });
    }

    /// <summary>Gera o XML do S-2220 (Monitoramento da Saude do Trabalhador — ASO).</summary>
    /// <param name="insumo">Snapshot do ASO.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS2220(InsumoS2220 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        return EscritorXmlEvento.Escrever("evtMonit", Ns("evtMonit/v_S_01_03_00"), idEvento, w =>
        {
            EscreverIdeEventoNaoPeriodico(w);
            EscreverIdeEmpregador(w, insumo.Empregador);
            EscreverIdeVinculo(w, insumo.Vinculo);

            w.WriteStartElement("exMedOcup");
            w.WriteElementString("tpExameOcup", insumo.TpExameOcup.ToString(CultureInfo.InvariantCulture)); // // TODO(validar-oficial: dominio).
            w.WriteStartElement("aso");
            w.WriteElementString("dtAso", EscritorXmlEvento.Data(insumo.DataAso));
            w.WriteElementString("resAso", insumo.ResAso.ToString(CultureInfo.InvariantCulture)); // 1=apto,2=inapto. // TODO(validar-oficial).

            // exameComp: procedimentos/exames complementares realizados (Tabela 27).
            foreach (var codigo in insumo.ExamesComplementares)
            {
                w.WriteStartElement("exame");
                w.WriteElementString("codProcRealizado", codigo); // // TODO(validar-oficial: Tabela 27).
                w.WriteEndElement(); // exame
            }

            // medico: responsavel pelo ASO (CRM/UF). // TODO(validar-oficial: grupo medico x respMonit).
            w.WriteStartElement("medico");
            w.WriteElementString("nmMed", insumo.MedicoNome);
            w.WriteElementString("nrCRM", insumo.MedicoNrCrm);
            w.WriteElementString("ufCRM", insumo.MedicoUfCrm);
            w.WriteEndElement(); // medico
            w.WriteEndElement(); // aso
            w.WriteEndElement(); // exMedOcup
        });
    }

    /// <summary>Gera o XML do S-2240 (Condicoes Ambientais do Trabalho — Agentes Nocivos).</summary>
    /// <param name="insumo">Snapshot das condicoes ambientais.</param>
    /// <param name="idEvento">Atributo Id do evento.</param>
    /// <returns>XML (UTF-8) do evento.</returns>
    public static byte[] GerarS2240(InsumoS2240 insumo, string idEvento)
    {
        ArgumentNullException.ThrowIfNull(insumo);
        return EscritorXmlEvento.Escrever("evtExpRisco", Ns("evtExpRisco/v_S_01_03_00"), idEvento, w =>
        {
            EscreverIdeEventoNaoPeriodico(w);
            EscreverIdeEmpregador(w, insumo.Empregador);
            EscreverIdeVinculo(w, insumo.Vinculo);

            w.WriteStartElement("infoExpRisco");
            w.WriteElementString("dtIniCondicao", EscritorXmlEvento.Data(insumo.InicioCondicao));
            if (insumo.FimCondicao is { } fim)
            {
                w.WriteElementString("dtFimCondicao", EscritorXmlEvento.Data(fim));
            }

            w.WriteStartElement("infoAmb");
            w.WriteElementString("dscSetor", insumo.SetorAtividade); // // TODO(validar-oficial: localAmb/dscSetor).
            w.WriteEndElement(); // infoAmb

            // agNoc: cada agente nocivo do periodo (Tabela 23) com intensidade e EPC/EPI.
            foreach (var agente in insumo.Agentes)
            {
                w.WriteStartElement("agNoc");
                w.WriteElementString("codAgNoc", agente.CodAgNoc); // // TODO(validar-oficial: Tabela 23).
                w.WriteElementString("dscAgNoc", agente.Descricao);
                if (agente.Intensidade is { } intensidade)
                {
                    w.WriteElementString("intConc", EscritorXmlEvento.Valor(intensidade));
                    w.EscreverSeTiver("unMed", agente.UnidadeMedida); // // TODO(validar-oficial: unMed/Tabela 25).
                }

                w.WriteElementString("utilizEPC", agente.UtilizaEpc ? "S" : "N"); // // TODO(validar-oficial: dominio utilizEPC).
                w.WriteElementString("utilizEPI", agente.UtilizaEpi ? "S" : "N"); // // TODO(validar-oficial: dominio utilizEPI).
                w.WriteEndElement(); // agNoc
            }

            w.WriteEndElement(); // infoExpRisco
        });
    }

    private static void EscreverIdeEventoNaoPeriodico(XmlWriter w)
    {
        w.WriteStartElement("ideEvento");
        w.WriteElementString("indRetif", "1"); // // TODO(validar-oficial: 1=original, 2=retificacao).
        w.WriteElementString("procEmi", "1");
        w.WriteElementString("verProc", "Tensorroot.Gov-eSocial");
        w.WriteEndElement(); // ideEvento
    }

    private static void EscreverIdeEmpregador(XmlWriter w, InscricaoEmpregador empregador)
    {
        w.WriteStartElement("ideEmpregador");
        w.WriteElementString("tpInsc", empregador.TpInsc.ToString(CultureInfo.InvariantCulture));
        w.WriteElementString("nrInsc", empregador.NrInsc);
        w.WriteEndElement(); // ideEmpregador
    }

    private static void EscreverIdeVinculo(XmlWriter w, IdeVinculoSst vinculo)
    {
        // ideVinculo (cpfTrab + matricula): identifica o trabalhador/vinculo nos eventos nao-periodicos de SST.
        w.WriteStartElement("ideVinculo");
        w.WriteElementString("cpfTrab", vinculo.CpfTrab);
        w.WriteElementString("matricula", vinculo.Matricula);
        w.WriteEndElement(); // ideVinculo
    }
}
