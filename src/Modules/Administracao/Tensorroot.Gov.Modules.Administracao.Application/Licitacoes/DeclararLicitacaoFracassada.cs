using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Encerra o certame por inexistencia de proposta valida/habilitada (art. 71).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="Motivo">Motivacao do ato.</param>
public sealed record DeclararLicitacaoFracassadaCommand(Guid LicitacaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da declaracao de fracasso.</summary>
public sealed class DeclararLicitacaoFracassadaValidator : AbstractValidator<DeclararLicitacaoFracassadaCommand>
{
    /// <summary>Define as regras.</summary>
    public DeclararLicitacaoFracassadaValidator()
    {
        RuleFor(comando => comando.LicitacaoId).NotEmpty().WithMessage("Licitacao e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().WithMessage("Motivo e obrigatorio.");
    }
}

/// <summary>Handler da declaracao de licitacao fracassada.</summary>
public sealed class DeclararLicitacaoFracassadaHandler(
    ILicitacaoRepository licitacoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeclararLicitacaoFracassadaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DeclararLicitacaoFracassadaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        licitacao.DeclararFracassada(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
