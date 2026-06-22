using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;

/// <summary>Registra a rejeicao da declaracao pelo SICONFI (secao 5.4 das regras).</summary>
/// <param name="DeclaracaoFiscalId">Declaracao a rejeitar.</param>
/// <param name="Motivo">Motivo da rejeicao (registrado para trilha).</param>
public sealed record RejeitarDeclaracaoFiscalCommand(Guid DeclaracaoFiscalId, string Motivo) : ICommand;

/// <summary>Regras de validacao da rejeicao da declaracao fiscal.</summary>
public sealed class RejeitarDeclaracaoFiscalValidator : AbstractValidator<RejeitarDeclaracaoFiscalCommand>
{
    /// <summary>Define as regras.</summary>
    public RejeitarDeclaracaoFiscalValidator()
    {
        RuleFor(comando => comando.DeclaracaoFiscalId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler da rejeicao da declaracao fiscal.</summary>
public sealed class RejeitarDeclaracaoFiscalHandler(
    IDeclaracaoFiscalRepository declaracoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RejeitarDeclaracaoFiscalCommand>
{
    /// <inheritdoc />
    public async Task Handle(RejeitarDeclaracaoFiscalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var declaracao = await declaracoes
            .ObterPorIdAsync(new DeclaracaoFiscalId(request.DeclaracaoFiscalId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Declaracao fiscal nao encontrada.");

        declaracao.Rejeitar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
