using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Projecao de detalhe de um usuario para leitura.</summary>
/// <param name="Id">Identificador do usuario.</param>
/// <param name="Nome">Nome de exibicao.</param>
/// <param name="Email">E-mail de login.</param>
/// <param name="Ativo">Se esta habilitado a autenticar.</param>
/// <param name="PapeisIds">Papeis atribuidos.</param>
public sealed record UsuarioDetalhe(
    Guid Id,
    string Nome,
    string Email,
    bool Ativo,
    IReadOnlyCollection<Guid> PapeisIds);

/// <summary>Obtem o detalhe de um usuario (tenant-scoped).</summary>
/// <param name="UsuarioId">Usuario a consultar.</param>
public sealed record ObterUsuarioQuery(Guid UsuarioId) : IQuery<UsuarioDetalhe>;

/// <summary>Handler da consulta de detalhe do usuario.</summary>
public sealed class ObterUsuarioHandler(IUsuarioRepository usuarios)
    : IQueryHandler<ObterUsuarioQuery, UsuarioDetalhe>
{
    /// <inheritdoc />
    public async Task<UsuarioDetalhe> Handle(ObterUsuarioQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        return new UsuarioDetalhe(
            usuario.Id.Value,
            usuario.Nome,
            usuario.Email.Valor,
            usuario.Ativo,
            usuario.Papeis.Select(papelId => papelId.Value).ToArray());
    }
}
