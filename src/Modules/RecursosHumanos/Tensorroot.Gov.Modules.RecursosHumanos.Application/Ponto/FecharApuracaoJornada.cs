using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto;

/// <summary>
/// Fecha a apuracao de jornada (congela espelho/AEJ) e dispara, via Outbox, o gancho para a folha
/// traduzir extras/faltas em rubricas — sem recalcular a folha aqui.
/// </summary>
/// <param name="ApuracaoPontoId">Apuracao a fechar.</param>
public sealed record FecharApuracaoJornadaCommand(Guid ApuracaoPontoId) : ICommand;

/// <summary>Regras de validacao do fechamento da apuracao.</summary>
public sealed class FecharApuracaoJornadaValidator : AbstractValidator<FecharApuracaoJornadaCommand>
{
    /// <summary>Define as regras.</summary>
    public FecharApuracaoJornadaValidator()
        => RuleFor(c => c.ApuracaoPontoId).NotEmpty().WithMessage("Apuracao e obrigatoria.");
}

/// <summary>Handler do fechamento da apuracao de jornada.</summary>
public sealed class FecharApuracaoJornadaHandler(
    IApuracaoPontoRepository apuracoes,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<FecharApuracaoJornadaCommand>
{
    /// <inheritdoc />
    public async Task Handle(FecharApuracaoJornadaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apuracao = await apuracoes.ObterPorIdAsync(new ApuracaoPontoId(request.ApuracaoPontoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Apuracao de ponto nao encontrada.");

        apuracao.Fechar(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
