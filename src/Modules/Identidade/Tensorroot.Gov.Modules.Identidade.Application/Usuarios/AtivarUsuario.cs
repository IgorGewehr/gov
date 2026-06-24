using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Habilita um usuario a autenticar.</summary>
/// <param name="UsuarioId">Usuario a ativar.</param>
public sealed record AtivarUsuarioCommand(Guid UsuarioId) : ICommand;

/// <summary>Regras de validacao da ativacao de usuario.</summary>
public sealed class AtivarUsuarioValidator : AbstractValidator<AtivarUsuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public AtivarUsuarioValidator() => RuleFor(comando => comando.UsuarioId).NotEmpty();
}

/// <summary>
/// Handler da ativacao de usuario. SEGURANCA (W10.6 ID-2): exige escopo administrativo sobre a UO do
/// alvo e anti-escalacao I4 (<see cref="AutorizacaoAdminUsuario"/>) — um admin de sub-UO nao reativa
/// usuario fora do seu escopo.
/// </summary>
public sealed class AtivarUsuarioHandler(
    IUsuarioRepository usuarios,
    AutorizacaoAdminUsuario autorizacao,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AtivarUsuarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtivarUsuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        // Deny-by-default: escopo/I4 antes de mutar o estado de ativacao do alvo.
        await autorizacao.GarantirPodeAgirSobreAsync(usuario, "AtivarUsuario", cancellationToken).ConfigureAwait(false);

        usuario.Ativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
