using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>Registra uma alergia no historico de um paciente ativo (I-8).</summary>
/// <param name="PacienteId">Paciente alvo.</param>
/// <param name="Substancia">Substancia/agente (obrigatorio).</param>
/// <param name="Gravidade">Gravidade da reacao.</param>
public sealed record RegistrarAlergiaCommand(
    Guid PacienteId,
    string Substancia,
    string Gravidade) : ICommand;

/// <summary>Regras de validacao do registro de alergia.</summary>
public sealed class RegistrarAlergiaValidator : AbstractValidator<RegistrarAlergiaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAlergiaValidator()
    {
        RuleFor(comando => comando.PacienteId).NotEmpty().WithMessage("Paciente e obrigatorio.");
        RuleFor(comando => comando.Substancia)
            .NotEmpty()
            .MaximumLength(Alergia.ComprimentoSubstancia)
            .WithMessage("Substancia da alergia e obrigatoria (max. 120).");
    }
}

/// <summary>Handler do registro de alergia.</summary>
public sealed class RegistrarAlergiaHandler(
    IPacienteRepository pacientes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarAlergiaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarAlergiaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paciente = await pacientes.ObterPorIdAsync(new PacienteId(request.PacienteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        paciente.RegistrarAlergia(request.Substancia, request.Gravidade, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
