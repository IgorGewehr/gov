using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>Confirma o cadastro do paciente apos validacao positiva do CNS no CADSUS (I-10).</summary>
/// <param name="PacienteId">Paciente a confirmar.</param>
public sealed record ConfirmarCadastroNoCadsusCommand(Guid PacienteId) : ICommand;

/// <summary>Regras de validacao da confirmacao de cadastro no CADSUS.</summary>
public sealed class ConfirmarCadastroNoCadsusValidator : AbstractValidator<ConfirmarCadastroNoCadsusCommand>
{
    /// <summary>Define as regras.</summary>
    public ConfirmarCadastroNoCadsusValidator()
    {
        RuleFor(comando => comando.PacienteId).NotEmpty().WithMessage("Paciente e obrigatorio.");
    }
}

/// <summary>Handler da confirmacao de cadastro no CADSUS.</summary>
public sealed class ConfirmarCadastroNoCadsusHandler(
    IPacienteRepository pacientes,
    ICadsusGateway cadsus,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmarCadastroNoCadsusCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConfirmarCadastroNoCadsusCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paciente = await pacientes.ObterPorIdAsync(new PacienteId(request.PacienteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");

        if (!await cadsus.ValidarCnsAsync(paciente.Cns, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("CADSUS nao confirmou o CNS do paciente.");
        }

        paciente.ConfirmarCadastro();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
