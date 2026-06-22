using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Internal;

/// <summary>
/// Resolve o ESCOPO EFETIVO de um usuario — a visao rica <c>(permissao -> conjunto de UOs)</c>
/// (MODELO §10.3): cruza as <see cref="AtribuicaoDePapel"/> vigentes do usuario com as permissoes
/// dos seus papeis e expande cada escopo de UO (<c>IncluiSubunidades</c>) na arvore do tenant
/// (invariante I5). Reutilizada pela consulta de permissoes efetivas, pela autenticacao (que embute
/// a projecao PLANA nas claims) e pela resolucao do escopo por requisicao (filtro de UO).
/// </summary>
internal static class CalculadoraPermissoesEfetivas
{
    /// <summary>
    /// Calcula o escopo efetivo do usuario no instante informado. Carrega os papeis referenciados
    /// pelas atribuicoes e a arvore de UOs do tenant; delega a regra de uniao/expansao ao dominio
    /// (<see cref="EscopoEfetivo.Calcular"/>).
    /// </summary>
    /// <param name="usuario">Usuario alvo.</param>
    /// <param name="papeis">Repositorio de papeis (tenant-scoped).</param>
    /// <param name="unidades">Repositorio de UOs (tenant-scoped).</param>
    /// <param name="instante">Momento de referencia para a vigencia das atribuicoes.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Escopo efetivo (permissao -> UOs) do usuario.</returns>
    public static async Task<EscopoEfetivo> ResolverEscopoAsync(
        Usuario usuario,
        IPapelRepository papeis,
        IUnidadeRepository unidades,
        DateTimeOffset instante,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(papeis);
        ArgumentNullException.ThrowIfNull(unidades);

        if (usuario.Atribuicoes.Count == 0)
        {
            return EscopoEfetivo.Vazio;
        }

        var papeisIds = usuario.Atribuicoes.Select(atribuicao => atribuicao.PapelId).Distinct().ToArray();
        var dosPapeis = await papeis.ObterPorIdsAsync(papeisIds, cancellationToken).ConfigureAwait(false);
        var permissoesPorPapel = dosPapeis.ToDictionary(
            papel => papel.Id,
            papel => papel.Permissoes);

        var todasUnidades = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);
        var arvore = ArvoreUnidades.Construir(todasUnidades);

        return EscopoEfetivo.Calcular(usuario.Atribuicoes, permissoesPorPapel, arvore, instante);
    }

    /// <summary>
    /// Resolve as permissoes efetivas na projecao PLANA (uniao, distinta e ordenada) — usada na
    /// BORDA HTTP, onde a claim <c>perm</c> continua por permissao (compatibilidade preservada).
    /// </summary>
    /// <param name="usuario">Usuario alvo.</param>
    /// <param name="papeis">Repositorio de papeis (tenant-scoped).</param>
    /// <param name="unidades">Repositorio de UOs (tenant-scoped).</param>
    /// <param name="instante">Momento de referencia para a vigencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Conjunto ordenado e distinto de permissoes efetivas.</returns>
    public static async Task<IReadOnlyList<string>> ResolverAsync(
        Usuario usuario,
        IPapelRepository papeis,
        IUnidadeRepository unidades,
        DateTimeOffset instante,
        CancellationToken cancellationToken)
    {
        var escopo = await ResolverEscopoAsync(usuario, papeis, unidades, instante, cancellationToken).ConfigureAwait(false);
        return escopo.Permissoes;
    }
}
