using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>Item (etapa) do cronograma físico-financeiro de uma obra.</summary>
/// <param name="Ordem">Ordem sequencial da etapa.</param>
/// <param name="Descricao">Descrição da etapa.</param>
/// <param name="PercentualFisicoPrevisto">Peso físico previsto (0–100).</param>
/// <param name="ValorPrevisto">Valor previsto.</param>
/// <param name="DataPrevistaInicio">Data prevista de início.</param>
/// <param name="DataPrevistaFim">Data prevista de término.</param>
public sealed record EtapaCronogramaInput(
    int Ordem,
    string Descricao,
    decimal PercentualFisicoPrevisto,
    decimal ValorPrevisto,
    DateOnly DataPrevistaInicio,
    DateOnly DataPrevistaFim);

/// <summary>Define o cronograma físico-financeiro (curva S) de uma obra planejada (I-3).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="Etapas">Etapas do cronograma.</param>
public sealed record DefinirCronogramaCommand(Guid ObraId, IReadOnlyList<EtapaCronogramaInput> Etapas) : ICommand;

/// <summary>Regras de validação da definição de cronograma.</summary>
public sealed class DefinirCronogramaValidator : AbstractValidator<DefinirCronogramaCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirCronogramaValidator()
    {
        RuleFor(comando => comando.ObraId).NotEmpty();
        RuleFor(comando => comando.Etapas).NotEmpty().WithMessage("O cronograma exige ao menos uma etapa.");
        RuleForEach(comando => comando.Etapas).ChildRules(etapa =>
        {
            etapa.RuleFor(item => item.Ordem).GreaterThan(0);
            etapa.RuleFor(item => item.Descricao).NotEmpty().MaximumLength(200);
            etapa.RuleFor(item => item.PercentualFisicoPrevisto).GreaterThan(0).LessThanOrEqualTo(100);
            etapa.RuleFor(item => item.ValorPrevisto).GreaterThan(0);
            etapa.RuleFor(item => item.DataPrevistaFim).GreaterThanOrEqualTo(item => item.DataPrevistaInicio);
        });
    }
}

/// <summary>Handler da definição de cronograma.</summary>
public sealed class DefinirCronogramaHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<DefinirCronogramaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DefinirCronogramaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        var etapas = request.Etapas
            .Select(input => EtapaCronograma.Criar(
                input.Ordem,
                input.Descricao,
                input.PercentualFisicoPrevisto,
                ValorMonetario.De(input.ValorPrevisto),
                input.DataPrevistaInicio,
                input.DataPrevistaFim))
            .ToList();

        obra.DefinirCronograma(etapas);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Emite a Ordem de Início de Serviço (Planejada → EmExecucao).</summary>
/// <param name="ObraId">Obra alvo.</param>
/// <param name="Data">Data de emissão da ordem.</param>
public sealed record EmitirOrdemInicioCommand(Guid ObraId, DateOnly Data) : ICommand;

/// <summary>Handler da emissão da ordem de início.</summary>
public sealed class EmitirOrdemInicioHandler(IObraRepository obras, IUnitOfWork unitOfWork)
    : ICommandHandler<EmitirOrdemInicioCommand>
{
    /// <inheritdoc />
    public async Task Handle(EmitirOrdemInicioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var obra = await obras.ObterPorIdAsync(new ObraId(request.ObraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Obra não encontrada.");

        obra.EmitirOrdemInicio(request.Data);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
