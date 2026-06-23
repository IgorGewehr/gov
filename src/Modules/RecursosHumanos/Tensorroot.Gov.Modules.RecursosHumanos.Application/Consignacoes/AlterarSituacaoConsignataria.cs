using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

/// <summary>Suspende uma consignataria (bloqueia novas averbacoes).</summary>
/// <param name="ConsignatariaId">Consignataria a suspender.</param>
public sealed record SuspenderConsignatariaCommand(Guid ConsignatariaId) : ICommand;

/// <summary>Reativa uma consignataria suspensa.</summary>
/// <param name="ConsignatariaId">Consignataria a reativar.</param>
public sealed record ReativarConsignatariaCommand(Guid ConsignatariaId) : ICommand;

/// <summary>Regras de validacao da suspensao de consignataria.</summary>
public sealed class SuspenderConsignatariaValidator : AbstractValidator<SuspenderConsignatariaCommand>
{
    /// <summary>Define as regras.</summary>
    public SuspenderConsignatariaValidator()
        => RuleFor(c => c.ConsignatariaId).NotEmpty().WithMessage("Consignataria e obrigatoria.");
}

/// <summary>Regras de validacao da reativacao de consignataria.</summary>
public sealed class ReativarConsignatariaValidator : AbstractValidator<ReativarConsignatariaCommand>
{
    /// <summary>Define as regras.</summary>
    public ReativarConsignatariaValidator()
        => RuleFor(c => c.ConsignatariaId).NotEmpty().WithMessage("Consignataria e obrigatoria.");
}

/// <summary>Handler da suspensao de consignataria.</summary>
public sealed class SuspenderConsignatariaHandler(
    IConsignatariaRepository consignatarias,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SuspenderConsignatariaCommand>
{
    /// <inheritdoc />
    public async Task Handle(SuspenderConsignatariaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var consignataria = await consignatarias.ObterPorIdAsync(new ConsignatariaId(request.ConsignatariaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Consignataria nao encontrada.");
        consignataria.Suspender();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da reativacao de consignataria.</summary>
public sealed class ReativarConsignatariaHandler(
    IConsignatariaRepository consignatarias,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReativarConsignatariaCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReativarConsignatariaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var consignataria = await consignatarias.ObterPorIdAsync(new ConsignatariaId(request.ConsignatariaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Consignataria nao encontrada.");
        consignataria.Reativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
