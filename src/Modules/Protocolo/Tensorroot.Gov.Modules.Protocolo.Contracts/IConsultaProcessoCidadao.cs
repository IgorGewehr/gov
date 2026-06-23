namespace Tensorroot.Gov.Modules.Protocolo.Contracts;

/// <summary>
/// Porta de LEITURA cidada do modulo Protocolo (cross-module via Contracts — CLAUDE.md §2). Expoe ao
/// Portal do Cidadao SOMENTE consultas pelo DOCUMENTO do interessado (CPF/CNPJ ja resolvido server-side
/// pelo modulo Cidadao a partir do principal autenticado). O Portal nunca conhece a entidade interna
/// <c>Processo</c>; passa apenas os digitos do documento, e o proprio Protocolo resolve no tenant.
/// <para>
/// SEGURANCA: o resultado respeita o <c>NivelDeAcesso</c> do processo — processos NAO publicos
/// (Restrito/Sigiloso) NAO sao expostos no portal mesmo ao proprio interessado (decisao de produto:
/// portal so trafega o que e publico ao titular). A consulta por <c>processoId</c> REVALIDA a
/// titularidade (interessado == documento resolvido) e o nivel de acesso (anti-IDOR): processo de
/// outro interessado ou nao-publico retorna <c>null</c> (indistinguivel de inexistente).
/// </para>
/// </summary>
public interface IConsultaProcessoCidadao
{
    /// <summary>
    /// Lista os processos PUBLICOS de que o cidadao identificado pelo <paramref name="documento"/> e o
    /// interessado, no tenant atual. Read-only.
    /// </summary>
    /// <param name="documento">CPF/CNPJ (somente digitos) do proprio cidadao, resolvido server-side.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Processos do interessado (vazio se nao houver).</returns>
    Task<IReadOnlyList<MeuProcessoDto>> ObterMeusProcessosAsync(
        string documento,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtem um processo por id REVALIDANDO a titularidade (interessado == documento) e o nivel de
    /// acesso. Retorna <c>null</c> quando o processo nao existe no tenant, nao e do interessado, ou nao
    /// e publico ao titular — indistinguivel, para nao vazar existencia.
    /// </summary>
    /// <param name="documento">CPF/CNPJ (somente digitos) do proprio cidadao, resolvido server-side.</param>
    /// <param name="processoId">Identificador do processo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O processo (com movimentacoes) ou <c>null</c>.</returns>
    Task<MeuProcessoDetalheDto?> ObterMeuProcessoAsync(
        string documento,
        Guid processoId,
        CancellationToken cancellationToken);
}

/// <summary>Resumo de um processo do proprio cidadao (projecao de leitura).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="Nup">Numero Unico de Protocolo.</param>
/// <param name="Classificacao">Classe documental.</param>
/// <param name="Situacao">Situacao atual (ex.: "EmTramitacao").</param>
/// <param name="DataAutuacao">Data da autuacao.</param>
public sealed record MeuProcessoDto(
    Guid ProcessoId,
    string Nup,
    string Classificacao,
    string Situacao,
    DateOnly DataAutuacao);

/// <summary>Movimentacao (tramitacao) de um processo (projecao de leitura cidada).</summary>
/// <param name="Data">Data da movimentacao.</param>
/// <param name="Observacao">Observacao da tramitacao, quando houver.</param>
public sealed record MinhaMovimentacaoDto(DateOnly Data, string? Observacao);

/// <summary>Detalhe de um processo do proprio cidadao (projecao de leitura).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="Nup">Numero Unico de Protocolo.</param>
/// <param name="Classificacao">Classe documental.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="DataAutuacao">Data da autuacao.</param>
/// <param name="PrazoFim">Data-limite do prazo legal/administrativo.</param>
/// <param name="Movimentacoes">Linha do tempo de tramitacoes.</param>
public sealed record MeuProcessoDetalheDto(
    Guid ProcessoId,
    string Nup,
    string Classificacao,
    string Situacao,
    DateOnly DataAutuacao,
    DateOnly PrazoFim,
    IReadOnlyList<MinhaMovimentacaoDto> Movimentacoes);
