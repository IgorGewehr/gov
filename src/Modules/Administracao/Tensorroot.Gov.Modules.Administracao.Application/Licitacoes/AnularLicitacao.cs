using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Anula o certame por ilegalidade (art. 71). Exige motivacao do vicio.</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="Motivo">Motivacao do ato administrativo (vicio de legalidade).</param>
public sealed record AnularLicitacaoCommand(Guid LicitacaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da anulacao.</summary>
public sealed class AnularLicitacaoValidator : AbstractValidator<AnularLicitacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AnularLicitacaoValidator()
    {
        RuleFor(comando => comando.LicitacaoId).NotEmpty().WithMessage("Licitacao e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().WithMessage("Motivo e obrigatorio.");
    }
}

/// <summary>Handler da anulacao de licitacao.</summary>
public sealed class AnularLicitacaoHandler(
    ILicitacaoRepository licitacoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AnularLicitacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AnularLicitacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        licitacao.Anular(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
