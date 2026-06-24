using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>Avanço de uma etapa atribuído a uma medição.</summary>
/// <param name="EtapaId">Etapa medida.</param>
/// <param name="PercentualFisicoNoPeriodo">Avanço físico da etapa no período (0–100).</param>
/// <param name="ValorNoPeriodo">Valor medido da etapa no período.</param>
public sealed record AvancoEtapaInput(Guid EtapaId, decimal PercentualFisicoNoPeriodo, decimal ValorNoPeriodo);

/// <summary>Registra um boletim de medição em rascunho (I-7/I-7b).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="CompetenciaAno">Ano da competência.</param>
/// <param name="CompetenciaMes">Mês da competência (1–12).</param>
/// <param name="PeriodoInicio">Início do período medido.</param>
/// <param name="PeriodoFim">Fim do período medido.</param>
/// <param name="Avancos">Avanços por etapa.</param>
public sealed record RegistrarMedicaoCommand(
    Guid ObraId,
    int CompetenciaAno,
    int CompetenciaMes,
    DateOnly PeriodoInicio,
    DateOnly PeriodoFim,
    IReadOnlyList<AvancoEtapaInput> Avancos) : ICommand<Guid>;

/// <summary>Regras de validação do registro de medição.</summary>
public sealed class RegistrarMedicaoValidator : AbstractValidator<RegistrarMedicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarMedicaoValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.CompetenciaAno).InclusiveBetween(2000, 2100);
        RuleFor(comando => comando.CompetenciaMes).InclusiveBetween(1, 12);
        RuleFor(comando => comando.PeriodoFim).GreaterThanOrEqualTo(comando => comando.PeriodoInicio);
        RuleFor(comando => comando.Avancos).NotEmpty().WithMessage("A medição exige ao menos um avanço de etapa.");
        RuleForEach(comando => comando.Avancos).ChildRules(avanco =>
        {
            avanco.RuleFor(item => item.EtapaId).NotEmpty();
            avanco.RuleFor(item => item.PercentualFisicoNoPeriodo).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
            avanco.RuleFor(item => item.ValorNoPeriodo).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>Handler do registro de medição.</summary>
public sealed class RegistrarMedicaoHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarMedicaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarMedicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        var avancos = request.Avancos
            .Select(input => new Obra.AvancoEtapa(
                new EtapaCronogramaId(input.EtapaId),
                input.PercentualFisicoNoPeriodo,
                ValorMonetario.De(input.ValorNoPeriodo)))
            .ToList();

        var medicaoId = obra.RegistrarMedicao(
            request.CompetenciaAno,
            request.CompetenciaMes,
            request.PeriodoInicio,
            request.PeriodoFim,
            avancos);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return medicaoId.Value;
    }
}
