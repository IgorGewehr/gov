using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Impede um usuario de autenticar (desligamento).</summary>
/// <param name="UsuarioId">Usuario a desativar.</param>
public sealed record DesativarUsuarioCommand(Guid UsuarioId) : ICommand;

/// <summary>Regras de validacao da desativacao de usuario.</summary>
public sealed class DesativarUsuarioValidator : AbstractValidator<DesativarUsuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public DesativarUsuarioValidator() => RuleFor(comando => comando.UsuarioId).NotEmpty();
}

/// <summary>
/// Handler da desativacao de usuario. SEGURANCA (W10.6 ID-2): exige escopo administrativo sobre a UO
/// do alvo e anti-escalacao I4 (<see cref="AutorizacaoAdminUsuario"/>) — barra a negacao de servico
/// administrativa (admin de sub-UO desativando o admin-raiz) e desativacao fora do escopo.
/// </summary>
public sealed class DesativarUsuarioHandler(
    IUsuarioRepository usuarios,
    AutorizacaoAdminUsuario autorizacao,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DesativarUsuarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(DesativarUsuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        // Deny-by-default: escopo/I4 antes de mutar o estado de ativacao do alvo.
        await autorizacao.GarantirPodeAgirSobreAsync(usuario, "DesativarUsuario", cancellationToken).ConfigureAwait(false);

        usuario.Desativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
