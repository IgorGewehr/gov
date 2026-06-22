using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Dotacoes;

/// <summary>Reforça (crédito suplementar) uma dotação.</summary>
/// <param name="DotacaoId">Identificador da dotação.</param>
/// <param name="Valor">Valor do reforço.</param>
public sealed record ReforcarDotacaoCommand(Guid DotacaoId, decimal Valor) : ICommand;

/// <summary>Anula crédito de uma dotação.</summary>
/// <param name="DotacaoId">Identificador da dotação.</param>
/// <param name="Valor">Valor a anular.</param>
public sealed record AnularCreditoDotacaoCommand(Guid DotacaoId, decimal Valor) : ICommand;

/// <summary>Regras de validação do reforço de dotação.</summary>
public sealed class ReforcarDotacaoValidator : AbstractValidator<ReforcarDotacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public ReforcarDotacaoValidator() => RuleFor(c => c.Valor).GreaterThan(0m);
}

/// <summary>Regras de validação da anulação de crédito.</summary>
public sealed class AnularCreditoDotacaoValidator : AbstractValidator<AnularCreditoDotacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AnularCreditoDotacaoValidator() => RuleFor(c => c.Valor).GreaterThan(0m);
}

/// <summary>Handler do reforço de dotação.</summary>
public sealed class ReforcarDotacaoHandler(IDotacaoOrcamentariaRepository dotacoes, IUnitOfWork unitOfWork)
    : ICommandHandler<ReforcarDotacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReforcarDotacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dotacao = await dotacoes.ObterPorIdAsync(new DotacaoOrcamentariaId(request.DotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dotacao nao encontrada.");

        dotacao.Reforcar(ValorMonetario.De(request.Valor));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da anulação de crédito de dotação.</summary>
public sealed class AnularCreditoDotacaoHandler(IDotacaoOrcamentariaRepository dotacoes, IUnitOfWork unitOfWork)
    : ICommandHandler<AnularCreditoDotacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AnularCreditoDotacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dotacao = await dotacoes.ObterPorIdAsync(new DotacaoOrcamentariaId(request.DotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dotacao nao encontrada.");

        dotacao.AnularCredito(ValorMonetario.De(request.Valor));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
