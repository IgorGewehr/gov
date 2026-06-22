using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Identidade;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;

/// <summary>
/// Registra (append-only) um acesso ao conteudo sigiloso do prontuario na trilha imutavel
/// (quem/quando/por que — I-7, I-8). Sem Integration Event, para preservar o sigilo (I-9).
/// <para>
/// LG-1: o usuario do acesso NAO vem do cliente — e o principal autenticado (claim <c>sub</c> via
/// <see cref="ICurrentUser"/>), derivado no handler. O cliente so informa o <see cref="MotivoAcesso"/>.
/// </para>
/// </summary>
/// <param name="ProntuarioId">Prontuario acessado.</param>
/// <param name="MotivoAcesso">Justificativa obrigatoria do acesso.</param>
public sealed record RegistrarAcessoProntuarioCommand(
    Guid ProntuarioId,
    string MotivoAcesso) : ICommand;

/// <summary>Regras de validacao do registro de acesso ao prontuario.</summary>
public sealed class RegistrarAcessoProntuarioValidator : AbstractValidator<RegistrarAcessoProntuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAcessoProntuarioValidator()
    {
        RuleFor(comando => comando.ProntuarioId).NotEmpty().WithMessage("Identificador do prontuario e obrigatorio.");
        RuleFor(comando => comando.MotivoAcesso).NotEmpty().MaximumLength(400).WithMessage("Motivo de acesso ao prontuario e obrigatorio.");
    }
}

/// <summary>
/// Handler do registro de acesso ao prontuario (trilha imutavel; sem Integration Event — I-9).
/// LG-1: usuario derivado do principal (claim <c>sub</c>), nunca do cliente.
/// </summary>
public sealed class RegistrarAcessoProntuarioHandler(
    IProntuarioSuasRepository prontuarios,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarAcessoProntuarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarAcessoProntuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // LG-1: identidade derivada do principal (claim 'sub'), nao do cliente. Falha-alto sem auth.
        var usuarioId = UsuarioDoAcesso.Resolver(currentUser);

        var prontuario = await prontuarios.ObterPorIdAsync(new ProntuarioSuasId(request.ProntuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Prontuario nao encontrado.");

        var agoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        prontuario.RegistrarAcesso(usuarioId, request.MotivoAcesso, agoraUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
