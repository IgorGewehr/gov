namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>
/// Resolve a identificação do ente para o SICONFI: o <c>Cod.Siconfi</c> da MSC, que é o código IBGE do
/// município (7 dígitos) seguido do sufixo <c>"EX"</c> do Poder Executivo (Regras Gerais MSC 2026 — a MSC é
/// ÚNICA por município, enviada SÓ pelo Executivo). Parametrizado por tenant/exercício (nunca <i>hardcoded</i>
/// — CLAUDE.md §7), pois o IBGE é dado do ente.
/// </summary>
public interface IIdentificacaoEnteSiconfi
{
    /// <summary>
    /// Obtém o <c>Cod.Siconfi</c> do ente: código IBGE (7 dígitos) + <c>"EX"</c> (ex.: <c>4312104EX</c>).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O <c>Cod.Siconfi</c> do ente para a coluna da MSC.</returns>
    Task<string> ObterCodigoSiconfiAsync(CancellationToken cancellationToken);
}
