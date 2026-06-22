using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>Registra uma condicao de saude (CID-10/CIAP-2) no historico de um paciente ativo (I-7).</summary>
/// <param name="PacienteId">Paciente alvo.</param>
/// <param name="Codigo">Codigo CID-10/CIAP-2 (obrigatorio).</param>
/// <param name="Descricao">Descricao da condicao.</param>
public sealed record RegistrarCondicaoDeSaudeCommand(
    Guid PacienteId,
    string Codigo,
    string Descricao) : ICommand;

/// <summary>Regras de validacao do registro de condicao de saude.</summary>
public sealed class RegistrarCondicaoDeSaudeValidator : AbstractValidator<RegistrarCondicaoDeSaudeCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarCondicaoDeSaudeValidator()
    {
        RuleFor(comando => comando.PacienteId).NotEmpty().WithMessage("Paciente e obrigatorio.");
        RuleFor(comando => comando.Codigo)
            .NotEmpty()
            .MaximumLength(CondicaoDeSaude.ComprimentoCodigo)
            .WithMessage("Codigo CID-10/CIAP-2 e obrigatorio (max. 10).");
    }
}

/// <summary>Handler do registro de condicao de saude.</summary>
public sealed class RegistrarCondicaoDeSaudeHandler(
    IPacienteRepository pacientes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarCondicaoDeSaudeCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarCondicaoDeSaudeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paciente = await pacientes.ObterPorIdAsync(new PacienteId(request.PacienteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        paciente.RegistrarCondicaoDeSaude(request.Codigo, request.Descricao, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
