using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;

/// <summary>Calcula a folha: consolida totais e aplica o abate-teto por servidor (I-5/I-6).</summary>
/// <param name="FolhaDePagamentoId">Folha a calcular.</param>
public sealed record CalcularFolhaCommand(Guid FolhaDePagamentoId) : ICommand;

/// <summary>Regras de validacao do calculo de folha.</summary>
public sealed class CalcularFolhaValidator : AbstractValidator<CalcularFolhaCommand>
{
    /// <summary>Define as regras.</summary>
    public CalcularFolhaValidator()
    {
        RuleFor(comando => comando.FolhaDePagamentoId)
            .NotEmpty()
            .WithMessage("Folha e obrigatoria.");
    }
}

/// <summary>Handler do calculo de folha (aplica o teto remuneratorio parametrizavel por tenant).</summary>
public sealed class CalcularFolhaHandler(
    IFolhaDePagamentoRepository folhas,
    IParametrosFolhaProvider parametros,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CalcularFolhaCommand>
{
    /// <inheritdoc />
    public async Task Handle(CalcularFolhaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folha = await folhas.ObterPorIdAsync(new FolhaDePagamentoId(request.FolhaDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Folha nao encontrada.");

        var config = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // I-5/I-6: o agregado consolida totais e reaplica o abate-teto.
        folha.Calcular(config.TetoRemuneratorio, hoje, config.CodigoRubricaAbateTeto);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
