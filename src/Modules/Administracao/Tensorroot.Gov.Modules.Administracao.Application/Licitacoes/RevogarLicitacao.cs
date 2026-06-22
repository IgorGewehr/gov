using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Revoga o certame por conveniencia/oportunidade (art. 71). Exige motivacao.</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="Motivo">Motivacao do ato administrativo.</param>
public sealed record RevogarLicitacaoCommand(Guid LicitacaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da revogacao.</summary>
public sealed class RevogarLicitacaoValidator : AbstractValidator<RevogarLicitacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RevogarLicitacaoValidator()
    {
        RuleFor(comando => comando.LicitacaoId).NotEmpty().WithMessage("Licitacao e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().WithMessage("Motivo e obrigatorio.");
    }
}

/// <summary>Handler da revogacao de licitacao.</summary>
public sealed class RevogarLicitacaoHandler(
    ILicitacaoRepository licitacoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RevogarLicitacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RevogarLicitacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        licitacao.Revogar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
