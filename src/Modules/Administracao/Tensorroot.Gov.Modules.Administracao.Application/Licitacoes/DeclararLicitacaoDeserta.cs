using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Encerra o certame por ausencia total de interessados (art. 71).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
public sealed record DeclararLicitacaoDesertaCommand(Guid LicitacaoId) : ICommand;

/// <summary>Regras de validacao da declaracao de desercao.</summary>
public sealed class DeclararLicitacaoDesertaValidator : AbstractValidator<DeclararLicitacaoDesertaCommand>
{
    /// <summary>Define as regras.</summary>
    public DeclararLicitacaoDesertaValidator()
    {
        RuleFor(comando => comando.LicitacaoId).NotEmpty().WithMessage("Licitacao e obrigatoria.");
    }
}

/// <summary>Handler da declaracao de licitacao deserta.</summary>
public sealed class DeclararLicitacaoDesertaHandler(
    ILicitacaoRepository licitacoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeclararLicitacaoDesertaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DeclararLicitacaoDesertaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        licitacao.DeclararDeserta();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
