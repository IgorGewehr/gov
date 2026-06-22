using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>Inativa o cadastro de um paciente ativo (obito/transferencia/duplicidade) — terminal (I-11).</summary>
/// <param name="PacienteId">Paciente a inativar.</param>
/// <param name="Motivo">Motivo da inativacao.</param>
public sealed record InativarPacienteCommand(Guid PacienteId, string Motivo) : ICommand;

/// <summary>Regras de validacao da inativacao de paciente.</summary>
public sealed class InativarPacienteValidator : AbstractValidator<InativarPacienteCommand>
{
    /// <summary>Define as regras.</summary>
    public InativarPacienteValidator()
    {
        RuleFor(comando => comando.PacienteId).NotEmpty().WithMessage("Paciente e obrigatorio.");
        RuleFor(comando => comando.Motivo).NotEmpty().WithMessage("Motivo da inativacao e obrigatorio.");
    }
}

/// <summary>Handler da inativacao de paciente.</summary>
public sealed class InativarPacienteHandler(
    IPacienteRepository pacientes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<InativarPacienteCommand>
{
    /// <inheritdoc />
    public async Task Handle(InativarPacienteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paciente = await pacientes.ObterPorIdAsync(new PacienteId(request.PacienteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");

        paciente.Inativar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
