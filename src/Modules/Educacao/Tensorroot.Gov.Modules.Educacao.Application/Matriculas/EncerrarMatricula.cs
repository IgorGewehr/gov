using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Contracts;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Matriculas;

/// <summary>Encerra uma matricula Ativa por conclusao da etapa/ano ou por abandono escolar.</summary>
/// <param name="MatriculaId">Identificador da matricula a encerrar.</param>
/// <param name="Motivo">Motivo do encerramento (<see cref="MotivoEncerramento.Conclusao"/> ou <see cref="MotivoEncerramento.Abandono"/>).</param>
public sealed record EncerrarMatriculaCommand(Guid MatriculaId, MotivoEncerramento Motivo) : ICommand;

/// <summary>Regras de validacao do encerramento de matricula.</summary>
public sealed class EncerrarMatriculaValidator : AbstractValidator<EncerrarMatriculaCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarMatriculaValidator()
    {
        RuleFor(comando => comando.MatriculaId).NotEmpty().WithMessage("Identificador da matricula obrigatorio.");
        RuleFor(comando => comando.Motivo).IsInEnum().WithMessage("Motivo de encerramento invalido.");
    }
}

/// <summary>Handler do encerramento de matricula.</summary>
public sealed class EncerrarMatriculaHandler(
    IMatriculaRepository matriculas,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<EncerrarMatriculaCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarMatriculaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var matricula = await matriculas.ObterPorIdAsync(new MatriculaId(request.MatriculaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Matricula nao encontrada.");

        // Regra de dominio: exige situacao Ativa (I-5); estado terminal nao admite transicao (I-9).
        switch (request.Motivo)
        {
            case MotivoEncerramento.Conclusao:
                matricula.Concluir();
                break;
            case MotivoEncerramento.Abandono:
                matricula.RegistrarAbandono();
                break;
            default:
                throw new InvalidOperationException($"Motivo de encerramento invalido: {request.Motivo}.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new MatriculaEncerradaIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            matricula.Id.Value,
            request.Motivo.ToString());

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
