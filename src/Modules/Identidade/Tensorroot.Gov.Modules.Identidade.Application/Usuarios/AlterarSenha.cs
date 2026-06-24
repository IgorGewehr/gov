using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
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

/// <summary>
/// Handler da alteracao de senha. SEGURANCA CRITICA (W10.6 ID-1, P0): reset de senha e o vetor de
/// tomada de conta — antes de trocar o hash, exige que o administrador atual COBRA o escopo do alvo
/// e o DOMINE (anti-escalacao I4 via <see cref="AutorizacaoAdminUsuario"/>). Sem isso, um admin de
/// sub-UO resetaria a senha do admin-raiz e autenticaria como ele.
/// </summary>
public sealed class AlterarSenhaHandler(
    IUsuarioRepository usuarios,
    ISenhaHasher hasher,
    AutorizacaoAdminUsuario autorizacao,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AlterarSenhaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AlterarSenhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        // Deny-by-default: escopo administrativo sobre a UO do alvo + anti-escalacao I4 (nao reseta a
        // senha de quem detem papel/permissao que o admin nao detem). Lanca 403 e audita se negar.
        await autorizacao.GarantirPodeAgirSobreAsync(usuario, "AlterarSenha", cancellationToken).ConfigureAwait(false);

        usuario.TrocarSenha(hasher.Hash(request.NovaSenha));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
