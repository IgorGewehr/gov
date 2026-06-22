using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;

/// <summary>
/// Registra (append-only) um acesso ao conteudo sigiloso do prontuario na trilha imutavel
/// (quem/quando/por que — I-7, I-8). Sem Integration Event, para preservar o sigilo (I-9).
/// </summary>
/// <param name="ProntuarioId">Prontuario acessado.</param>
/// <param name="UsuarioId">Usuario que acessou.</param>
/// <param name="MotivoAcesso">Justificativa obrigatoria do acesso.</param>
public sealed record RegistrarAcessoProntuarioCommand(
    Guid ProntuarioId,
    Guid UsuarioId,
    string MotivoAcesso) : ICommand;

/// <summary>Regras de validacao do registro de acesso ao prontuario.</summary>
public sealed class RegistrarAcessoProntuarioValidator : AbstractValidator<RegistrarAcessoProntuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAcessoProntuarioValidator()
    {
        RuleFor(comando => comando.ProntuarioId).NotEmpty().WithMessage("Identificador do prontuario e obrigatorio.");
        RuleFor(comando => comando.UsuarioId).NotEmpty().WithMessage("Usuario do acesso e obrigatorio.");
        RuleFor(comando => comando.MotivoAcesso).NotEmpty().MaximumLength(400).WithMessage("Motivo de acesso ao prontuario e obrigatorio.");
    }
}

/// <summary>Handler do registro de acesso ao prontuario (trilha imutavel; sem Integration Event — I-9).</summary>
public sealed class RegistrarAcessoProntuarioHandler(
    IProntuarioSuasRepository prontuarios,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarAcessoProntuarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarAcessoProntuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prontuario = await prontuarios.ObterPorIdAsync(new ProntuarioSuasId(request.ProntuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Prontuario nao encontrado.");

        var agoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        prontuario.RegistrarAcesso(request.UsuarioId, request.MotivoAcesso, agoraUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
