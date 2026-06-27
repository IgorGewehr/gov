namespace Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;

/// <summary>
/// Hipotese autorizadora do credenciamento (Lei 14.133/2021, art. 79, I a III). O credenciamento e
/// forma auxiliar de contratacao (art. 78, I) processada por inexigibilidade de licitacao (art. 74, IV):
/// como TODOS os interessados que satisfacam as condicoes podem ser contratados, inexiste competicao e,
/// portanto, inviabiliza-se a licitacao. A hipotese define o regime de demanda/selecao do edital.
/// </summary>
public enum HipoteseCredenciamento
{
    /// <summary>
    /// Art. 79, I — contratacao PARALELA E NAO EXCLUDENTE: a Administracao contrata, simultaneamente,
    /// todos os credenciados, sem disputa entre eles (ex.: rede de prestadores de saude/exames). A demanda
    /// e distribuida a todos os interessados habilitados.
    /// </summary>
    ParalelaNaoExcludente = 1,

    /// <summary>
    /// Art. 79, II — contratacao COM SELECAO a criterio de terceiros: a escolha do credenciado e feita
    /// pelo beneficiario direto do servico (ex.: o usuario/paciente escolhe entre os credenciados).
    /// </summary>
    SelecaoCriterioBeneficiario = 2,

    /// <summary>
    /// Art. 79, III — MERCADOS FLUIDOS: a flutuacao constante do valor da prestacao e das condicoes de
    /// contratacao inviabiliza a competicao por preco (ex.: passagens aereas, combustiveis). A contratacao
    /// se da a precos fixados pela Administracao/mercado, por adesao dos credenciados.
    /// </summary>
    MercadosFluidos = 3,
}

/// <summary>
/// Situacao (estado) do edital de credenciamento no ciclo de vida procedimental (Lei 14.133/2021,
/// art. 79, paragrafo unico: chamamento publico permanentemente aberto, com possibilidade de ingresso
/// a qualquer tempo enquanto vigente o edital).
/// </summary>
public enum SituacaoCredenciamento
{
    /// <summary>Rascunho: edital em elaboracao, itens/condicoes em cadastro, ainda sem chamamento publicado.</summary>
    EmElaboracao = 1,

    /// <summary>Chamamento publico publicado e ABERTO: inscricoes recebidas de forma permanente (art. 79, par. unico).</summary>
    ChamamentoAberto = 2,

    /// <summary>Inscricoes temporariamente suspensas (ato motivado), sem encerrar o credenciamento.</summary>
    Suspenso = 3,

    /// <summary>Credenciamento encerrado por decurso de vigencia ou conveniencia (terminal): nao recebe novas inscricoes.</summary>
    Encerrado = 4,

    /// <summary>Edital anulado por ilegalidade (terminal).</summary>
    Anulado = 5,

    /// <summary>Edital revogado por conveniencia/oportunidade (terminal).</summary>
    Revogado = 6,
}

/// <summary>
/// Situacao da inscricao/credenciamento de um interessado (Lei 14.133/2021, art. 79 c/c art. 80 — analise
/// documental e habilitacao do interessado, com ingresso a qualquer tempo).
/// </summary>
public enum SituacaoCredenciado
{
    /// <summary>Inscricao protocolada, aguardando analise documental/habilitacao.</summary>
    EmAnalise = 1,

    /// <summary>Interessado habilitado e CREDENCIADO: apto a ser contratado/demandado.</summary>
    Credenciado = 2,

    /// <summary>Inscricao indeferida por nao atendimento das condicoes do edital (analise reprovada).</summary>
    Indeferido = 3,

    /// <summary>Credenciamento suspenso (descredenciamento temporario por descumprimento sanavel).</summary>
    Suspenso = 4,

    /// <summary>Descredenciado (terminal): por pedido proprio, descumprimento ou sancao impeditiva.</summary>
    Descredenciado = 5,
}
