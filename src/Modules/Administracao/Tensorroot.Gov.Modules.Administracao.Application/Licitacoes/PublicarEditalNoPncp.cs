using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>
/// Publica o edital da licitacao no Portal Nacional de Contratacoes Publicas (art. 174). A
/// publicacao efetiva no Portal e feita pelo handler do Outbox (cliente PNCP resiliente).
/// </summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="NumeroEditalPncp">Identificador da contratacao no PNCP.</param>
public sealed record PublicarEditalNoPncpCommand(Guid LicitacaoId, string NumeroEditalPncp) : ICommand;

/// <summary>Regras de validacao da publicacao do edital no PNCP.</summary>
public sealed class PublicarEditalNoPncpValidator : AbstractValidator<PublicarEditalNoPncpCommand>
{
    /// <summary>Define as regras.</summary>
    public PublicarEditalNoPncpValidator()
    {
        RuleFor(comando => comando.LicitacaoId).NotEmpty().WithMessage("Licitacao e obrigatoria.");
        RuleFor(comando => comando.NumeroEditalPncp)
            .NotEmpty()
            .MaximumLength(60)
            .WithMessage("Numero do edital no PNCP e obrigatorio (max. 60).");
    }
}

/// <summary>Handler da publicacao do edital no PNCP.</summary>
public sealed class PublicarEditalNoPncpHandler(
    ILicitacaoRepository licitacoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<PublicarEditalNoPncpCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarEditalNoPncpCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        licitacao.PublicarEditalPncp(request.NumeroEditalPncp);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
