namespace Tensorroot.Gov.Modules.Administracao.Application.LicitaCon;

/// <summary>
/// Parametros (do tenant/orgao) necessarios para a remessa LicitaCon ao TCE-RS que NAO vivem no agregado
/// <c>Licitacao</c> — codigo do orgao no TCE-RS, razao social e o de/para de codigos de dominio do leiaute
/// 1.4 (modalidade, tipo de objeto, fase, criterio). Resolvidos pela borda (configuracao por tenant); sem
/// numero magico no agregado (CLAUDE.md §16). // TODO(M10-validate): tabela de codigos de dominio do
/// leiaute 1.4 (CD_TIPO_MODALIDADE, TP_OBJETO, CD_TIPO_FASE_ATUAL etc.) contra o PDF oficial.
/// </summary>
/// <param name="CodigoOrgao">Codigo do orgao no TCE-RS (CD_ORGAO).</param>
/// <param name="NomeOrgao">Nome/razao social do orgao (NM_ORGAO).</param>
/// <param name="CodigoTipoModalidade">Codigo da modalidade no leiaute (CD_TIPO_MODALIDADE).</param>
/// <param name="TipoObjeto">Codigo do tipo de objeto (TP_OBJETO: compras/servicos/obras...).</param>
/// <param name="CodigoTipoFaseAtual">Codigo da fase atual do certame (CD_TIPO_FASE_ATUAL).</param>
/// <param name="TipoNivelJulgamento">Codigo do nivel de julgamento (TP_NIVEL_JULGAMENTO: global/lote/item).</param>
/// <param name="NumeroProcesso">Numero do processo administrativo (NR_PROCESSO).</param>
/// <param name="AnoProcesso">Ano do processo (ANO_PROCESSO).</param>
/// <param name="NumeroLicitacao">Numero da licitacao no ente (NR_LICITACAO).</param>
/// <param name="AnoLicitacao">Ano da licitacao (ANO_LICITACAO).</param>
public sealed record RemessaLicitaConParametros(
    int CodigoOrgao,
    string NomeOrgao,
    int CodigoTipoModalidade,
    int TipoObjeto,
    int CodigoTipoFaseAtual,
    int TipoNivelJulgamento,
    string NumeroProcesso,
    int AnoProcesso,
    int NumeroLicitacao,
    int AnoLicitacao);

/// <summary>
/// Um arquivo da remessa LicitaCon (nome oficial + bytes UTF-8/BOM do CSV). Os 14 nomes do leiaute 1.4
/// estao em <see cref="ArquivosLicitaCon"/> como constantes.
/// </summary>
/// <param name="Nome">Nome do arquivo (ex.: "LICITACAO.csv").</param>
/// <param name="Conteudo">Bytes do CSV (BOM + corpo, CRLF).</param>
/// <param name="QuantidadeLinhas">Quantidade de linhas de dados (sem cabecalho).</param>
public sealed record ArquivoRemessaLicitaCon(string Nome, byte[] Conteudo, int QuantidadeLinhas);

/// <summary>
/// Resultado da geracao da remessa LicitaCon: os 14 arquivos CSV do leiaute 1.4. A TRANSMISSAO efetiva ao
/// TCE-RS (empacotamento e envio ao e-Validador/Processo Eletronico, com credencial) e
/// // TODO(M10) — depende de credenciamento. Aqui produzimos os arquivos validaveis (idempotentes pelo
/// conteudo da licitacao).
/// </summary>
/// <param name="Arquivos">Os 14 arquivos da remessa, na ordem do leiaute.</param>
public sealed record RemessaLicitaCon(IReadOnlyList<ArquivoRemessaLicitaCon> Arquivos);

/// <summary>Nomes oficiais dos 14 arquivos CSV da remessa LicitaCon 1.4 (e-Validador TCE-RS).</summary>
public static class ArquivosLicitaCon
{
    /// <summary>Pessoas (PESSOAS).</summary>
    public const string Pessoas = "PESSOAS.csv";

    /// <summary>Membros de consorcio (MEMBRO_CONSORCIO).</summary>
    public const string MembroConsorcio = "MEMBRO_CONSORCIO.csv";

    /// <summary>Comissao de licitacao (COMISSAO).</summary>
    public const string Comissao = "COMISSAO.csv";

    /// <summary>Membros da comissao (MEMBRO_COMISSAO).</summary>
    public const string MembroComissao = "MEMBRO_COMISSAO.csv";

    /// <summary>Licitacao (LICITACAO).</summary>
    public const string Licitacao = "LICITACAO.csv";

    /// <summary>Licitantes (LICITANTE).</summary>
    public const string Licitante = "LICITANTE.csv";

    /// <summary>Dotacoes orcamentarias (DOTACAO_LICITACAO).</summary>
    public const string DotacaoLicitacao = "DOTACAO_LICITACAO.csv";

    /// <summary>Eventos do certame (EVENTO_LICITACAO).</summary>
    public const string EventoLicitacao = "EVENTO_LICITACAO.csv";

    /// <summary>Lotes (LOTE).</summary>
    public const string Lote = "LOTE.csv";

    /// <summary>Itens (ITEM).</summary>
    public const string Item = "ITEM.csv";

    /// <summary>Propostas (PROPOSTA).</summary>
    public const string Proposta = "PROPOSTA.csv";

    /// <summary>Propostas por lote (LOTE_PROPOSTA).</summary>
    public const string LoteProposta = "LOTE_PROPOSTA.csv";

    /// <summary>Propostas por item (ITEM_PROPOSTA).</summary>
    public const string ItemProposta = "ITEM_PROPOSTA.csv";

    /// <summary>Documentos da licitacao (DOCUMENTO_LICITACAO).</summary>
    public const string DocumentoLicitacao = "DOCUMENTO_LICITACAO.csv";
}
