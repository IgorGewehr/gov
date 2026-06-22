using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

/// <summary>Registra a frequencia (presenca/falta) de uma aula/dia em um diario aberto (I-3).</summary>
/// <param name="DiarioClasseId">Diario a registrar.</param>
/// <param name="Data">Data da frequencia.</param>
/// <param name="Presente">Presenca do aluno.</param>
/// <param name="CargaHorariaAula">Carga horaria da aula (positiva).</param>
public sealed record RegistrarFrequenciaCommand(
    Guid DiarioClasseId,
    DateOnly Data,
    bool Presente,
    int CargaHorariaAula) : ICommand;

/// <summary>Regras de validacao do registro de frequencia.</summary>
public sealed class RegistrarFrequenciaValidator : AbstractValidator<RegistrarFrequenciaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarFrequenciaValidator()
    {
        RuleFor(comando => comando.DiarioClasseId).NotEmpty().WithMessage("Identificador do diario obrigatorio.");
        RuleFor(comando => comando.Data).NotEmpty().WithMessage("Data da frequencia obrigatoria.");
        RuleFor(comando => comando.CargaHorariaAula).GreaterThan(0).WithMessage("Carga horaria da aula deve ser maior que zero.");
    }
}

/// <summary>Handler do registro de frequencia.</summary>
public sealed class RegistrarFrequenciaHandler(IDiarioClasseRepository diarios, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarFrequenciaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarFrequenciaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diario = await diarios.ObterPorIdAsync(new DiarioClasseId(request.DiarioClasseId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Diario nao encontrado.");

        diario.RegistrarFrequencia(request.Data, request.Presente, request.CargaHorariaAula);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
