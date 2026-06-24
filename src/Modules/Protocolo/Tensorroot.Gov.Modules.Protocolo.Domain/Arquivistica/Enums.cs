namespace Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

/// <summary>
/// Natureza da atividade na classificacao arquivistica (CONARQ): atividades-MEIO tem codigo de
/// classificacao NACIONAL; atividades-FIM sao proprias do municipio/ente (RS).
/// </summary>
public enum AtividadeMeioOuFim
{
    /// <summary>Atividade-meio (administracao geral) — codigo nacional CONARQ.</summary>
    Meio = 1,

    /// <summary>Atividade-fim (finalistica do ente) — definida pelo municipio/RS.</summary>
    Fim = 2,
}

/// <summary>
/// Destinacao final do documento/processo apos a guarda (TTD/CONARQ): eliminacao OU guarda permanente.
/// </summary>
public enum Destinacao
{
    /// <summary>Eliminacao apos o prazo de guarda (exige edital + termo — Res. CONARQ 40/2014).</summary>
    Eliminacao = 1,

    /// <summary>Guarda permanente (nunca elimina — valor probatorio/historico).</summary>
    GuardaPermanente = 2,
}

/// <summary>
/// Evento a partir do qual a contagem do prazo de guarda comeca (parametro da regra, por tenant).
/// </summary>
public enum EventoContagem
{
    /// <summary>A partir da data de autuacao do processo.</summary>
    DataAutuacao = 1,

    /// <summary>A partir da data de arquivamento (padrao).</summary>
    DataArquivamento = 2,

    /// <summary>A partir da aprovacao das contas (ex.: prestacao de contas ao TCE).</summary>
    AprovacaoContas = 3,
}

/// <summary>
/// Estado da ficha de destinacao de um processo arquivado (ciclo de vida da decisao de destinacao).
/// </summary>
public enum EstadoDestinacao
{
    /// <summary>Aguardando o decurso do prazo de guarda.</summary>
    AguardandoPrazo = 1,

    /// <summary>Prazo decorrido; apto a eliminacao (depende de ato humano + edital + termo).</summary>
    AptoEliminar = 2,

    /// <summary>Guarda permanente (terminal — nunca elimina).</summary>
    Permanente = 3,

    /// <summary>Eliminacao autorizada por ato humano (RBAC), aguardando registro do termo.</summary>
    EliminacaoAutorizada = 4,

    /// <summary>Eliminado (terminal) — termo assinado/carimbado registrado sob WORM.</summary>
    Eliminado = 5,
}
