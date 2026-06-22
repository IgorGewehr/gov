using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Altera a senha de um usuario (a nova senha em claro e transformada em hash).</summary>
/// <param name="UsuarioId">Usuario alvo.</param>
/// <param name="NovaSenha">Nova senha em claro.</param>
public sealed record AlterarSenhaCommand(Guid UsuarioId, string NovaSenha) : ICommand;

/// <summary>Regras de validacao da alteracao de senha.</summary>
public sealed class AlterarSenhaValidator : AbstractValidator<AlterarSenhaCommand>
{
    /// <summary>Define as regras.</summary>
    public AlterarSenhaValidator()
    {
        RuleFor(comando => comando.UsuarioId).NotEmpty();
        RuleFor(comando => comando.NovaSenha).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}

/// <summary>Handler da alteracao de senha.</summary>
public sealed class AlterarSenhaHandler(
    IUsuarioRepository usuarios,
    ISenhaHasher hasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AlterarSenhaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AlterarSenhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        usuario.TrocarSenha(hasher.Hash(request.NovaSenha));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
