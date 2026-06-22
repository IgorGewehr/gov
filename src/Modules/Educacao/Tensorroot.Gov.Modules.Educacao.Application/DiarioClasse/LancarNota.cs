using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

/// <summary>Lanca a nota de um componente curricular em um periodo, em um diario aberto (I-3/I-8).</summary>
/// <param name="DiarioClasseId">Diario a lancar.</param>
/// <param name="ComponenteCurricularId">Componente curricular da nota.</param>
/// <param name="Periodo">Periodo de avaliacao.</param>
/// <param name="Valor">Valor da nota (0 a 10).</param>
public sealed record LancarNotaCommand(
    Guid DiarioClasseId,
    Guid ComponenteCurricularId,
    string Periodo,
    decimal Valor) : ICommand;

/// <summary>Regras de validacao do lancamento de nota.</summary>
public sealed class LancarNotaValidator : AbstractValidator<LancarNotaCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarNotaValidator()
    {
        RuleFor(comando => comando.DiarioClasseId).NotEmpty().WithMessage("Identificador do diario obrigatorio.");
        RuleFor(comando => comando.ComponenteCurricularId).NotEmpty().WithMessage("Componente curricular obrigatorio.");
        RuleFor(comando => comando.Periodo).NotEmpty().MaximumLength(20).WithMessage("Periodo obrigatorio (max. 20 caracteres).");
        RuleFor(comando => comando.Valor).InclusiveBetween(0, 10).WithMessage("Nota deve estar entre 0 e 10.");
    }
}

/// <summary>Handler do lancamento de nota.</summary>
public sealed class LancarNotaHandler(IDiarioClasseRepository diarios, IUnitOfWork unitOfWork)
    : ICommandHandler<LancarNotaCommand>
{
    /// <inheritdoc />
    public async Task Handle(LancarNotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diario = await diarios.ObterPorIdAsync(new DiarioClasseId(request.DiarioClasseId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Diario nao encontrado.");

        diario.LancarNota(new ComponenteCurricularId(request.ComponenteCurricularId), request.Periodo, request.Valor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
