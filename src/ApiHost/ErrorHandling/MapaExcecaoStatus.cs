using System.Globalization;
using FluentValidation;

namespace Tensorroot.Gov.ApiHost.ErrorHandling;

/// <summary>
/// Resultado puro do mapeamento de uma exceção para a borda HTTP (RFC 7807): status, título e,
/// quando aplicável (validação), os erros por campo. Não carrega detalhe sensível (sem stack trace).
/// </summary>
/// <param name="StatusCode">Status HTTP a responder.</param>
/// <param name="Titulo">Título curto e estável (campo <c>title</c> do problem+json).</param>
/// <param name="Detalhe">Mensagem segura para o cliente (campo <c>detail</c>) ou <c>null</c>.</param>
/// <param name="ErrosPorCampo">Erros de validação por campo (RFC 7807 §"errors"), ou <c>null</c>.</param>
internal sealed record ResultadoMapeamento(
    int StatusCode,
    string Titulo,
    string? Detalhe,
    IReadOnlyDictionary<string, string[]>? ErrosPorCampo);

/// <summary>
/// Mapeador PURO exceção→status HTTP. Vive no Composition Root (ApiHost) — a única camada que pode
/// conhecer a borda HTTP — e NÃO acopla os módulos entre si: classifica por (1) tipos-base de
/// framework (FluentValidation) e (2) TAXONOMIA POR NOME (convenção ubíqua PT-BR do projeto), em vez
/// de um switch frágil sobre cada tipo concreto de cada módulo. Assim, novas exceções de domínio que
/// sigam a convenção de nome já nascem mapeadas, sem tocar no ApiHost nem quebrar o isolamento (§2).
///
/// <para>
/// DECISÃO DELIBERADA: a resposta NUNCA vaza stack trace nem mensagem de exceção de fallback (nem em
/// Development) — o detalhe técnico fica só no log interno (Serilog, com CorrelationId/TenantId). Para
/// exceções de NEGÓCIO mapeadas (4xx), a <c>Message</c> é segura por construção (as mensagens do
/// domínio já são redigidas para o usuário/Tribunal) e é exposta como <c>detail</c>.
/// </para>
/// </summary>
internal static class MapaExcecaoStatus
{
    private const int Status400ValidacaoBad = StatusCodes.Status400BadRequest;
    private const int Status403Negacao = StatusCodes.Status403Forbidden;
    private const int Status404NaoEncontrado = StatusCodes.Status404NotFound;
    private const int Status409Conflito = StatusCodes.Status409Conflict;
    private const int Status422Invariante = StatusCodes.Status422UnprocessableEntity;
    private const int Status500Interno = StatusCodes.Status500InternalServerError;

    /// <summary>
    /// Mapeia a exceção para um <see cref="ResultadoMapeamento"/>. O fallback é 500 GENÉRICO, sem
    /// detalhe sensível — só o <c>traceId</c> (adicionado na borda) correlaciona ao log.
    /// </summary>
    /// <param name="excecao">Exceção capturada pelo manipulador global.</param>
    /// <returns>Status + título + detalhe seguro (+ erros de campo quando validação).</returns>
    public static ResultadoMapeamento Mapear(Exception excecao)
    {
        ArgumentNullException.ThrowIfNull(excecao);

        // (1) VALIDAÇÃO (FluentValidation, lançada pelo ValidationBehavior): 400 com erros por campo.
        if (excecao is ValidationException validacao)
        {
            var erros = validacao.Errors
                .GroupBy(falha => falha.PropertyName, StringComparer.Ordinal)
                .ToDictionary(
                    grupo => string.IsNullOrEmpty(grupo.Key) ? "_" : grupo.Key,
                    grupo => grupo.Select(falha => falha.ErrorMessage).ToArray(),
                    StringComparer.Ordinal);

            return new ResultadoMapeamento(
                Status400ValidacaoBad,
                "Requisição inválida.",
                "Um ou mais campos não passaram na validação.",
                erros);
        }

        var nome = excecao.GetType().Name;

        // (2) TAXONOMIA POR NOME (convenção ubíqua PT-BR). A ORDEM importa: negação primeiro (deny-by-
        //     default tem precedência), depois não-encontrado, depois invariante/conflito.

        // 403 — AUTORIZAÇÃO / NEGAÇÃO / DENY / CROSS-TENANT.
        if (EhNegacao(nome) || EhCrossTenant(excecao))
        {
            return new ResultadoMapeamento(
                Status403Negacao,
                "Acesso negado.",
                excecao.Message,
                ErrosPorCampo: null);
        }

        // 404 — NÃO ENCONTRADO. Detectado pelo TIPO (convenção de nome) OU pela MENSAGEM ubíqua PT-BR
        //       ("não encontrado"). A checagem por mensagem é deliberada: muitos handlers sinalizam o
        //       recurso ausente com um InvalidOperationException("... nao encontrado.") em vez de um
        //       tipo dedicado — sem isto, cairiam no 422 abaixo (regra de negócio) em vez do 404
        //       correto. Vem ANTES do ramo InvalidOperationException→422 (a ordem importa).
        if (Contem(nome, "NaoEncontrad") || Contem(nome, "NotFound") || Contem(nome, "Inexistente")
            || EhMensagemDeNaoEncontrado(excecao.Message))
        {
            return new ResultadoMapeamento(
                Status404NaoEncontrado,
                "Recurso não encontrado.",
                excecao.Message,
                ErrosPorCampo: null);
        }

        // 422 — VIOLAÇÃO DE INVARIANTE / REGRA DE NEGÓCIO de domínio. Escolhemos 422 (entidade não
        //       processável) e não 409: a maioria dessas exceções deriva de InvalidOperationException
        //       e representa regra de negócio reprovada (saldo insuficiente, partida desbalanceada,
        //       transição de estado inválida), não um conflito de concorrência/versão de recurso.
        if (excecao is InvalidOperationException || EhRegraDeNegocioPorNome(nome))
        {
            return new ResultadoMapeamento(
                Status422Invariante,
                "Regra de negócio violada.",
                excecao.Message,
                ErrosPorCampo: null);
        }

        // (3) FALLBACK — 500 GENÉRICO, SEM detalhe sensível. Só o traceId correlaciona ao log.
        return new ResultadoMapeamento(
            Status500Interno,
            "Erro interno.",
            "Ocorreu um erro inesperado ao processar a requisição. Use o traceId para correlacionar ao log.",
            ErrosPorCampo: null);
    }

    // 409 está reservado para conflito de concorrência/recurso quando uma exceção dedicada existir
    // (ver TODO no manipulador). Hoje nenhuma exceção do código sinaliza esse caso explicitamente,
    // então não inferimos 409 por heurística para não classificar errado uma regra de negócio.
    internal static int StatusConflitoReservado => Status409Conflito;

    private static bool EhNegacao(string nome)
        => Contem(nome, "SemVinculo")
        || Contem(nome, "NaoAutorizad")
        || Contem(nome, "AcessoNegado")
        || Contem(nome, "Proibid")
        || Contem(nome, "Forbidden")
        || Contem(nome, "NaoAplicavel")            // BaseLegalLgpdNaoAplicavelException (deny LGPD).
        || Contem(nome, "AutenticacaoFalhou");      // credenciais inválidas → tratado como negação.

    // "Não encontrado" pela MENSAGEM (linguagem ubíqua PT-BR do domínio): cobre as variações de
    // acento/caixa ("nao encontrado", "não encontrada", ...). As mensagens do domínio são redigidas
    // para o usuário/Tribunal (seguras por construção), então usá-las para classificar o status é
    // confiável e não vaza detalhe técnico.
    private static bool EhMensagemDeNaoEncontrado(string? mensagem)
        => !string.IsNullOrEmpty(mensagem)
        && (Contem(mensagem, "nao encontrad")   // sem acento (convenção de identificadores/strings)
         || Contem(mensagem, "não encontrad")   // com acento
         || Contem(mensagem, "inexistente"));

    private static bool EhRegraDeNegocioPorNome(string nome)
        => Contem(nome, "Invalid")
        || Contem(nome, "Invalida")
        || Contem(nome, "Insuficiente")
        || Contem(nome, "Desbalanceada")
        || Contem(nome, "Excedid")
        || Contem(nome, "Fechad")
        || Contem(nome, "Incompatibilidade")
        || Contem(nome, "Ausente")
        || Contem(nome, "NaoVigente");

    // Cross-tenant: hoje o guard (TenantSaveChangesInterceptor) lança InvalidOperationException com
    // mensagem "Gravação cross-tenant bloqueada". Detectamos pela mensagem para responder 403 (e não
    // 422), pois é uma NEGAÇÃO de segurança, não uma regra de negócio do domínio.
    private static bool EhCrossTenant(Exception excecao)
        => excecao.Message.Contains("cross-tenant", StringComparison.OrdinalIgnoreCase);

    private static bool Contem(string nome, string termo)
        => CultureInfo.InvariantCulture.CompareInfo.IndexOf(
               nome, termo, CompareOptions.IgnoreCase) >= 0;
}
