using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Obtem as permissoes efetivas (uniao das permissoes dos papeis) de um usuario.</summary>
/// <param name="UsuarioId">Usuario a consultar.</param>
public sealed record ObterPermissoesEfetivasDoUsuarioQuery(Guid UsuarioId) : IQuery<IReadOnlyList<string>>;

/// <summary>Handler da consulta de permissoes efetivas.</summary>
public sealed class ObterPermissoesEfetivasDoUsuarioHandler(
    IUsuarioRepository usuarios,
    IPapelRepository papeis,
    IUnidadeRepository unidades,
    TimeProvider clock)
    : IQueryHandler<ObterPermissoesEfetivasDoUsuarioQuery, IReadOnlyList<string>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> Handle(
        ObterPermissoesEfetivasDoUsuarioQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        return await CalculadoraPermissoesEfetivas
            .ResolverAsync(usuario, papeis, unidades, clock.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
    }
}
