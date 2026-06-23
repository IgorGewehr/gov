using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

/// <summary>
/// Fornece a <see cref="PoliticaDelegacao"/> vigente do tenant atual (AA-5/D3) — o teto de
/// profundidade de subdelegacao e PARAMETRIZAVEL por tenant (CLAUDE.md §7: regras de autorizacao
/// nunca hardcoded). Quando o tenant nao define um valor proprio, vale o padrao conservador.
/// </summary>
public interface IPoliticaDelegacaoProvider
{
    /// <summary>Obtem a politica de (sub)delegacao do tenant atual.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Politica de delegacao vigente (padrao se o tenant nao definiu).</returns>
    Task<PoliticaDelegacao> ObterAsync(CancellationToken cancellationToken);
}
