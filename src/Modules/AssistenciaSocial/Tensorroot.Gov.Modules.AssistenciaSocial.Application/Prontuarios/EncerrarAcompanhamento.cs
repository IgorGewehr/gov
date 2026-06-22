using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;

/// <summary>Encerra o acompanhamento familiar de um prontuario, com motivo (I-6, I-11).</summary>
/// <param name="ProntuarioId">Prontuario a encerrar.</param>
/// <param name="MotivoEncerramento">Motivo do encerramento.</param>
public sealed record EncerrarAcompanhamentoCommand(
    Guid ProntuarioId,
    string MotivoEncerramento) : ICommand;

/// <summary>Regras de validacao do encerramento de acompanhamento.</summary>
public sealed class EncerrarAcompanhamentoValidator : AbstractValidator<EncerrarAcompanhamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarAcompanhamentoValidator()
    {
        RuleFor(comando => comando.ProntuarioId).NotEmpty().WithMessage("Identificador do prontuario e obrigatorio.");
        RuleFor(comando => comando.MotivoEncerramento).NotEmpty().MaximumLength(400).WithMessage("Motivo do encerramento e obrigatorio.");
    }
}

/// <summary>
/// Handler do encerramento de acompanhamento. Muta o agregado e publica
/// <see cref="AcompanhamentoEncerradoIntegrationEvent"/> (so metadados — I-9).
/// </summary>
public sealed class EncerrarAcompanhamentoHandler(
    IProntuarioSuasRepository prontuarios,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<EncerrarAcompanhamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarAcompanhamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prontuario = await prontuarios.ObterPorIdAsync(new ProntuarioSuasId(request.ProntuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Prontuario nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        prontuario.EncerrarAcompanhamento(request.MotivoEncerramento, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new AcompanhamentoEncerradoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            prontuario.Id.Value,
            request.MotivoEncerramento);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
