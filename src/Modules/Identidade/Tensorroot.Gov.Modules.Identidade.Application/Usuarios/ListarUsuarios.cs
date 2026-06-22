using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Resumo de um usuario para listagem.</summary>
/// <param name="Id">Identificador do usuario.</param>
/// <param name="Nome">Nome de exibicao.</param>
/// <param name="Email">E-mail de login.</param>
/// <param name="Ativo">Se esta habilitado a autenticar.</param>
/// <param name="QuantidadePapeis">Quantidade de papeis atribuidos.</param>
public sealed record UsuarioResumo(Guid Id, string Nome, string Email, bool Ativo, int QuantidadePapeis);

/// <summary>Lista os usuarios do tenant atual.</summary>
public sealed record ListarUsuariosQuery : IQuery<IReadOnlyList<UsuarioResumo>>;

/// <summary>Handler da listagem de usuarios.</summary>
public sealed class ListarUsuariosHandler(IUsuarioRepository usuarios)
    : IQueryHandler<ListarUsuariosQuery, IReadOnlyList<UsuarioResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UsuarioResumo>> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lista = await usuarios.ListarAsync(cancellationToken).ConfigureAwait(false);

        return lista
            .Select(usuario => new UsuarioResumo(
                usuario.Id.Value,
                usuario.Nome,
                usuario.Email.Valor,
                usuario.Ativo,
                usuario.Papeis.Count))
            .ToList();
    }
}
