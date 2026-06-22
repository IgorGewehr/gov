using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Internal;

/// <summary>
/// Implementacao de <see cref="IResolvedorEscopoUnidade"/>: resolve as UOs legiveis de um usuario a
/// partir do seu escopo efetivo (delega ao calculo de dominio via
/// <see cref="CalculadoraPermissoesEfetivas"/>). Vive na Aplicacao para acessar o calculo interno;
/// e exposto a Infraestrutura apenas pela porta publica.
/// </summary>
public sealed class ResolvedorEscopoUnidade(
    IUsuarioRepository usuarios,
    IPapelRepository papeis,
    IUnidadeRepository unidades) : IResolvedorEscopoUnidade
{
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> ResolverUnidadesLegiveisAsync(
        Guid usuarioId,
        DateTimeOffset instante,
        CancellationToken cancellationToken)
    {
        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(usuarioId), cancellationToken).ConfigureAwait(false);
        if (usuario is null)
        {
            return [];
        }

        var escopo = await CalculadoraPermissoesEfetivas
            .ResolverEscopoAsync(usuario, papeis, unidades, instante, cancellationToken)
            .ConfigureAwait(false);

        var conjunto = new HashSet<Guid>();
        foreach (var permissao in escopo.Permissoes)
        {
            foreach (var uo in escopo.UnidadesDaPermissao(permissao))
            {
                conjunto.Add(uo.Value);
            }
        }

        return conjunto;
    }
}
