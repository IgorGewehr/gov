using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.LicitaCon;

/// <summary>
/// Gera a remessa LicitaCon 1.4 (e-Validador TCE-RS) — os 14 arquivos CSV — a partir do agregado
/// <see cref="Licitacao"/> e dos parametros do orgao/tenant. A estrutura de colunas espelha o leiaute
/// 1.4 publicado pelo TCE-RS (verificada ao vivo contra arquivo real do dados.tce.rs.gov.br). Funcao
/// PURA (sem I/O): produz os bytes dos arquivos; a TRANSMISSAO efetiva ao TCE-RS e // TODO(M10)
/// (credenciamento). Cobre os arquivos com dados disponiveis no agregado (LICITACAO, LOTE, LICITANTE,
/// PROPOSTA) e emite os demais com o cabecalho oficial (sem linhas) ate que o dominio modele esses
/// conceitos (comissao, dotacoes, itens granulares, documentos).
/// </summary>
public static class GeradorRemessaLicitaCon
{
    /// <summary>
    /// Constroi os 14 arquivos da remessa a partir da licitacao e dos parametros do orgao.
    /// </summary>
    /// <param name="licitacao">Licitacao a remeter (agregado completo: lotes, propostas, habilitacoes).</param>
    /// <param name="parametros">Parametros do orgao/tenant e codigos de dominio do leiaute.</param>
    /// <returns>Remessa com os 14 CSV (UTF-8/BOM, virgula, CRLF).</returns>
    public static RemessaLicitaCon Gerar(Licitacao licitacao, RemessaLicitaConParametros parametros)
    {
        ArgumentNullException.ThrowIfNull(licitacao);
        ArgumentNullException.ThrowIfNull(parametros);

        var arquivos = new List<ArquivoRemessaLicitaCon>
        {
            Vazio(ArquivosLicitaCon.Pessoas, ColunasPessoas),
            Vazio(ArquivosLicitaCon.MembroConsorcio, ColunasMembroConsorcio),
            Vazio(ArquivosLicitaCon.Comissao, ColunasComissao),
            Vazio(ArquivosLicitaCon.MembroComissao, ColunasMembroComissao),
            GerarLicitacao(licitacao, parametros),
            GerarLicitante(licitacao, parametros),
            Vazio(ArquivosLicitaCon.DotacaoLicitacao, ColunasDotacao),
            Vazio(ArquivosLicitaCon.EventoLicitacao, ColunasEvento),
            GerarLote(licitacao, parametros),
            Vazio(ArquivosLicitaCon.Item, ColunasItem),
            GerarProposta(licitacao, parametros),
            Vazio(ArquivosLicitaCon.LoteProposta, ColunasLoteProposta),
            Vazio(ArquivosLicitaCon.ItemProposta, ColunasItemProposta),
            Vazio(ArquivosLicitaCon.DocumentoLicitacao, ColunasDocumento),
        };

        return new RemessaLicitaCon(arquivos);
    }

    private static ArquivoRemessaLicitaCon GerarLicitacao(Licitacao licitacao, RemessaLicitaConParametros p)
    {
        var csv = new LicitaConCsvEscritor(ColunasLicitacao);
        var venc = licitacao.FornecedorVencedorId();
        // 65 colunas do LICITACAO 1.4 (ordem oficial). Campos sem dado no agregado vao vazios — o
        // e-Validador admite vazio nos opcionais; // TODO(M10-validate) obrigatoriedade por coluna.
        csv.AdicionarLinha(
            Int(p.CodigoOrgao),                 // CD_ORGAO
            p.NomeOrgao,                        // NM_ORGAO
            Int(p.NumeroLicitacao),             // NR_LICITACAO
            Int(p.AnoLicitacao),                // ANO_LICITACAO
            Int(p.CodigoTipoModalidade),        // CD_TIPO_MODALIDADE
            null, null, null,                   // NR_COMISSAO, ANO_COMISSAO, TP_COMISSAO
            p.NumeroProcesso,                   // NR_PROCESSO
            Int(p.AnoProcesso),                 // ANO_PROCESSO
            Int(p.TipoObjeto),                  // TP_OBJETO
            Int(p.CodigoTipoFaseAtual),         // CD_TIPO_FASE_ATUAL
            null,                               // TP_LICITACAO
            Int(p.TipoNivelJulgamento),         // TP_NIVEL_JULGAMENTO
            null, null, null, null,             // DT_AUTORIZACAO_ADESAO, TP_CARACTERISTICA_OBJETO, TP_NATUREZA, TP_REGIME_EXECUCAO
            LicitaConCsvEscritor.Booleano(false), // BL_PERMITE_SUBCONTRATACAO
            null, null, null,                   // TP_BENEFICIO_MICRO_EPP, TP_FORNECIMENTO, TP_ATUACAO_REGISTRO
            null, null,                         // NR_LICITACAO_ORIGINAL, ANO_LICITACAO_ORIGINAL
            null, null, null,                   // NR_ATA_REGISTRO_PRECO, DT_ATA_REGISTRO_PRECO, PC_TAXA_RISCO
            null, null, null,                   // TP_EXECUCAO, TP_DISPUTA, TP_PREQUALIFICACAO
            LicitaConCsvEscritor.Booleano(false), // BL_INVERSAO_FASES
            null, null, null,                   // TP_RESULTADO_GLOBAL, CNPJ_ORGAO_GERENCIADOR, NM_ORGAO_GERENCIADOR
            licitacao.Objeto,                   // DS_OBJETO
            null, null, null, null,             // CD_TIPO_FUNDAMENTACAO, NR_ARTIGO, DS_INCISO, DS_LEI
            null, null, null, null,             // DT_INICIO_INSCR_CRED, DT_FIM_INSCR_CRED, DT_INICIO_VIGEN_CRED, DT_FIM_VIGEN_CRED
            LicitaConCsvEscritor.Valor(licitacao.ValorEstimado.Valor), // VL_LICITACAO
            LicitaConCsvEscritor.Booleano(false), // BL_ORCAMENTO_SIGILOSO
            LicitaConCsvEscritor.Booleano(false), // BL_RECEBE_INSCRICAO_PER_VIG
            LicitaConCsvEscritor.Booleano(false), // BL_PERMITE_CONSORCIO
            null, null, null,                   // DT_ABERTURA, DT_HOMOLOGACAO, DT_ADJUDICACAO
            LicitaConCsvEscritor.Booleano(true),  // BL_LICIT_PROPRIA_ORGAO
            null, null,                         // TP_DOCUMENTO_FORNECEDOR, NR_DOCUMENTO_FORNECEDOR
            null,                               // TP_DOCUMENTO_VENCEDOR
            venc?.ToString(),                   // NR_DOCUMENTO_VENCEDOR (id do fornecedor vencedor — // TODO(M10) mapear ao CNPJ/CPF real)
            LicitaConCsvEscritor.Valor(venc is null ? null : licitacao.ValorAdjudicado()), // VL_HOMOLOGADO
            LicitaConCsvEscritor.Booleano(true),  // BL_GERA_DESPESA
            null, null, null,                   // DS_OBSERVACAO, PC_TX_ESTIMADA, PC_TX_HOMOLOGADA
            LicitaConCsvEscritor.Booleano(false), // BL_COMPARTILHADA
            LicitaConCsvEscritor.Booleano(false), // BL_COVID19
            null, null,                         // LINK_LICITACON_CIDADAO, DS_JUST_PRESENCIAL
            LicitaConCsvEscritor.Booleano(false)); // BL_PC_MIN_MULHERES_VIOLENCIA

        return Empacotar(ArquivosLicitaCon.Licitacao, csv);
    }

    private static ArquivoRemessaLicitaCon GerarLote(Licitacao licitacao, RemessaLicitaConParametros p)
    {
        var csv = new LicitaConCsvEscritor(ColunasLote);
        foreach (var lote in licitacao.Lotes)
        {
            csv.AdicionarLinha(
                Int(p.CodigoOrgao),             // CD_ORGAO
                Int(p.NumeroLicitacao),         // NR_LICITACAO
                Int(p.AnoLicitacao),            // ANO_LICITACAO
                Int(p.CodigoTipoModalidade),    // CD_TIPO_MODALIDADE
                Int(lote.Numero),               // NR_LOTE
                lote.Descricao,                 // DS_LOTE
                LicitaConCsvEscritor.Valor(lote.ValorEstimado.Valor), // VL_ESTIMADO
                null,                           // VL_HOMOLOGADO
                null,                           // TP_RESULTADO_LOTE
                null, null,                     // TP_DOCUMENTO, NR_DOCUMENTO (vencedor do lote)
                null, null,                     // TP_DOCUMENTO, NR_DOCUMENTO (2)
                null, null, null);              // TP_BENEFICIO_MICRO_EPP, PC_TX_ESTIMADA, PC_TX_HOMOLOGADA
        }

        return Empacotar(ArquivosLicitaCon.Lote, csv);
    }

    private static ArquivoRemessaLicitaCon GerarLicitante(Licitacao licitacao, RemessaLicitaConParametros p)
    {
        var csv = new LicitaConCsvEscritor(ColunasLicitante);
        // Um licitante por fornecedor distinto que apresentou proposta; resultado de habilitacao do agregado.
        var fornecedores = licitacao.Propostas.Select(prop => prop.FornecedorId).Distinct();
        foreach (var fornecedorId in fornecedores)
        {
            var habilitacao = licitacao.Habilitacoes
                .Where(h => h.FornecedorId == fornecedorId)
                .OrderByDescending(h => h.DataVerificacao)
                .ThenByDescending(h => h.Sequencia)
                .FirstOrDefault();
            var resultado = habilitacao?.Resultado switch
            {
                ResultadoHabilitacao.Habilitado => "1",
                ResultadoHabilitacao.Inabilitado => "2",
                _ => null,
            };

            csv.AdicionarLinha(
                Int(p.CodigoOrgao),             // CD_ORGAO
                Int(p.NumeroLicitacao),         // NR_LICITACAO
                Int(p.AnoLicitacao),            // ANO_LICITACAO
                Int(p.CodigoTipoModalidade),    // CD_TIPO_MODALIDADE
                null,                           // TP_DOCUMENTO (// TODO(M10) mapear ao CNPJ/CPF real do fornecedor)
                fornecedorId.ToString("N"),     // NR_DOCUMENTO
                null, null,                     // TP_DOCUMENTO, NR_DOCUMENTO (representante)
                null,                           // TP_CONDICAO
                resultado,                      // TP_RESULTADO_HABILITACAO
                LicitaConCsvEscritor.Booleano(false)); // BL_BENEFICIO_MICRO_EPP
        }

        return Empacotar(ArquivosLicitaCon.Licitante, csv);
    }

    private static ArquivoRemessaLicitaCon GerarProposta(Licitacao licitacao, RemessaLicitaConParametros p)
    {
        var csv = new LicitaConCsvEscritor(ColunasProposta);
        foreach (var proposta in licitacao.Propostas)
        {
            var resultado = proposta.Situacao switch
            {
                SituacaoProposta.Vencedora => "1",
                SituacaoProposta.Classificada => "2",
                SituacaoProposta.Desclassificada => "3",
                _ => null,
            };

            csv.AdicionarLinha(
                Int(p.CodigoOrgao),             // CD_ORGAO
                Int(p.NumeroLicitacao),         // NR_LICITACAO
                Int(p.AnoLicitacao),            // ANO_LICITACAO
                Int(p.CodigoTipoModalidade),    // CD_TIPO_MODALIDADE
                null,                           // TP_DOCUMENTO (// TODO(M10) mapear ao documento real)
                proposta.FornecedorId.ToString("N"), // NR_DOCUMENTO
                null,                           // DT_PROPOSTA
                resultado,                      // TP_RESULTADO_PROPOSTA
                LicitaConCsvEscritor.Valor(proposta.Valor.Valor), // VL_TOTAL_PROPOSTA
                null, null, null, null);        // PC_DESCONTO, VL_NOTA_TECNICA, DT_HOMOLOGACAO, PC_TX
        }

        return Empacotar(ArquivosLicitaCon.Proposta, csv);
    }

    private static ArquivoRemessaLicitaCon Vazio(string nome, string[] colunas)
        => Empacotar(nome, new LicitaConCsvEscritor(colunas));

    private static ArquivoRemessaLicitaCon Empacotar(string nome, LicitaConCsvEscritor csv)
        => new(nome, csv.SerializarBytes(), csv.QuantidadeLinhas);

    private static string Int(int valor) => LicitaConCsvEscritor.Inteiro(valor)!;

    // === Cabecalhos oficiais (leiaute 1.4 — verificados contra remessa real do TCE-RS) ===

    private static readonly string[] ColunasLicitacao =
    [
        "CD_ORGAO", "NM_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "NR_COMISSAO",
        "ANO_COMISSAO", "TP_COMISSAO", "NR_PROCESSO", "ANO_PROCESSO", "TP_OBJETO", "CD_TIPO_FASE_ATUAL",
        "TP_LICITACAO", "TP_NIVEL_JULGAMENTO", "DT_AUTORIZACAO_ADESAO", "TP_CARACTERISTICA_OBJETO",
        "TP_NATUREZA", "TP_REGIME_EXECUCAO", "BL_PERMITE_SUBCONTRATACAO", "TP_BENEFICIO_MICRO_EPP",
        "TP_FORNECIMENTO", "TP_ATUACAO_REGISTRO", "NR_LICITACAO_ORIGINAL", "ANO_LICITACAO_ORIGINAL",
        "NR_ATA_REGISTRO_PRECO", "DT_ATA_REGISTRO_PRECO", "PC_TAXA_RISCO", "TP_EXECUCAO", "TP_DISPUTA",
        "TP_PREQUALIFICACAO", "BL_INVERSAO_FASES", "TP_RESULTADO_GLOBAL", "CNPJ_ORGAO_GERENCIADOR",
        "NM_ORGAO_GERENCIADOR", "DS_OBJETO", "CD_TIPO_FUNDAMENTACAO", "NR_ARTIGO", "DS_INCISO", "DS_LEI",
        "DT_INICIO_INSCR_CRED", "DT_FIM_INSCR_CRED", "DT_INICIO_VIGEN_CRED", "DT_FIM_VIGEN_CRED",
        "VL_LICITACAO", "BL_ORCAMENTO_SIGILOSO", "BL_RECEBE_INSCRICAO_PER_VIG", "BL_PERMITE_CONSORCIO",
        "DT_ABERTURA", "DT_HOMOLOGACAO", "DT_ADJUDICACAO", "BL_LICIT_PROPRIA_ORGAO",
        "TP_DOCUMENTO_FORNECEDOR", "NR_DOCUMENTO_FORNECEDOR", "TP_DOCUMENTO_VENCEDOR",
        "NR_DOCUMENTO_VENCEDOR", "VL_HOMOLOGADO", "BL_GERA_DESPESA", "DS_OBSERVACAO", "PC_TX_ESTIMADA",
        "PC_TX_HOMOLOGADA", "BL_COMPARTILHADA", "BL_COVID19", "LINK_LICITACON_CIDADAO",
        "DS_JUST_PRESENCIAL", "BL_PC_MIN_MULHERES_VIOLENCIA",
    ];

    private static readonly string[] ColunasLote =
    [
        // NB: o leiaute 1.4 REPETE TP_DOCUMENTO/NR_DOCUMENTO (vencedor + 2º colocado) — nomes duplicados
        // sao intencionais e casam por POSICAO no e-Validador.
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "NR_LOTE", "DS_LOTE",
        "VL_ESTIMADO", "VL_HOMOLOGADO", "TP_RESULTADO_LOTE", "TP_DOCUMENTO", "NR_DOCUMENTO",
        "TP_DOCUMENTO", "NR_DOCUMENTO", "TP_BENEFICIO_MICRO_EPP", "PC_TX_ESTIMADA", "PC_TX_HOMOLOGADA",
    ];

    private static readonly string[] ColunasItem =
    [
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "NR_LOTE", "NR_ITEM",
        "NR_ITEM_ORIGINAL", "DS_ITEM", "QT_ITENS", "SG_UNIDADE_MEDIDA", "VL_UNITARIO_ESTIMADO",
        "VL_TOTAL_ESTIMADO", "DT_REF_VALOR_ESTIMADO", "PC_BDI_ESTIMADO", "PC_ENCARGOS_SOCIAIS_ESTIMADO",
        "CD_FONTE_REFERENCIA", "DS_FONTE_REFERENCIA", "TP_RESULTADO_ITEM", "VL_UNITARIO_HOMOLOGADO",
        "VL_TOTAL_HOMOLOGADO", "PC_BDI_HOMOLOGADO", "PC_ENCARGOS_SOCIAIS_HOMOLOGADO", "TP_ORCAMENTO",
        "CD_TIPO_FAMILIA", "CD_TIPO_SUBFAMILIA", "TP_DOCUMENTO", "NR_DOCUMENTO", "TP_DOCUMENTO",
        "NR_DOCUMENTO", "TP_BENEFICIO_MICRO_EPP", "PC_TX_ESTIMADA", "PC_TX_HOMOLOGADA", "BL_COVID19",
    ];

    private static readonly string[] ColunasProposta =
    [
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "TP_DOCUMENTO", "NR_DOCUMENTO",
        "DT_PROPOSTA", "TP_RESULTADO_PROPOSTA", "VL_TOTAL_PROPOSTA", "PC_DESCONTO", "VL_NOTA_TECNICA",
        "DT_HOMOLOGACAO", "PC_TX",
    ];

    private static readonly string[] ColunasLicitante =
    [
        // Leiaute 1.4 REPETE TP_DOCUMENTO/NR_DOCUMENTO (licitante + representante) — duplicidade por posicao.
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "TP_DOCUMENTO", "NR_DOCUMENTO",
        "TP_DOCUMENTO", "NR_DOCUMENTO", "TP_CONDICAO", "TP_RESULTADO_HABILITACAO",
        "BL_BENEFICIO_MICRO_EPP",
    ];

    private static readonly string[] ColunasPessoas =
    [
        "CD_ORGAO", "TP_DOCUMENTO", "NR_DOCUMENTO", "NM_PESSOA", "TP_PESSOA", "SG_UF", "CD_MUNICIPIO_IBGE",
        "NM_MUNICIPIO", "DS_ENDERECO", "DS_EMAIL", "NR_TELEFONE",
    ];

    private static readonly string[] ColunasMembroConsorcio =
    [
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "TP_DOCUMENTO_CONSORCIO",
        "NR_DOCUMENTO_CONSORCIO", "TP_DOCUMENTO_MEMBRO", "NR_DOCUMENTO_MEMBRO", "PC_PARTICIPACAO",
    ];

    private static readonly string[] ColunasComissao =
    [
        "CD_ORGAO", "NR_COMISSAO", "ANO_COMISSAO", "TP_COMISSAO", "DT_INICIO", "DT_FIM", "DS_FINALIDADE",
        "NR_ATO_NOMEACAO", "DT_ATO_NOMEACAO",
    ];

    private static readonly string[] ColunasMembroComissao =
    [
        "CD_ORGAO", "NR_COMISSAO", "ANO_COMISSAO", "TP_COMISSAO", "TP_DOCUMENTO", "NR_DOCUMENTO",
        "TP_FUNCAO_MEMBRO", "DT_INICIO", "DT_FIM",
    ];

    private static readonly string[] ColunasDotacao =
    [
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "NR_DOTACAO", "ANO_DOTACAO",
        "DS_DOTACAO", "VL_DOTACAO",
    ];

    private static readonly string[] ColunasEvento =
    [
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "TP_EVENTO", "DT_EVENTO",
        "DS_EVENTO", "NR_DOCUMENTO_RESPONSAVEL",
    ];

    private static readonly string[] ColunasLoteProposta =
    [
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "NR_LOTE", "TP_DOCUMENTO",
        "NR_DOCUMENTO", "VL_TOTAL_LOTE_PROPOSTA", "PC_DESCONTO", "TP_RESULTADO_LOTE_PROPOSTA",
    ];

    private static readonly string[] ColunasItemProposta =
    [
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "NR_LOTE", "NR_ITEM",
        "TP_DOCUMENTO", "NR_DOCUMENTO", "VL_UNITARIO_PROPOSTA", "VL_TOTAL_PROPOSTA",
        "TP_RESULTADO_ITEM_PROPOSTA", "PC_DESCONTO",
    ];

    private static readonly string[] ColunasDocumento =
    [
        "CD_ORGAO", "NR_LICITACAO", "ANO_LICITACAO", "CD_TIPO_MODALIDADE", "TP_DOCUMENTO_LICITACAO",
        "DS_DOCUMENTO", "DT_DOCUMENTO", "LINK_DOCUMENTO",
    ];
}
