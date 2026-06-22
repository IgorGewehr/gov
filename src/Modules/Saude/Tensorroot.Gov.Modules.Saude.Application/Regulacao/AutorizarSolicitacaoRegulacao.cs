using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Regulacao;

/// <summary>Autoriza (defere) uma solicitacao de regulacao, reservando a vaga no SISREG e consumindo cota.</summary>
/// <param name="SolicitacaoRegulacaoId">Solicitacao a autorizar.</param>
public sealed record AutorizarSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId) : ICommand;

/// <summary>Regras de validacao da autorizacao de solicitacao de regulacao.</summary>
public sealed class AutorizarSolicitacaoRegulacaoValidator : AbstractValidator<AutorizarSolicitacaoRegulacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AutorizarSolicitacaoRegulacaoValidator()
    {
        RuleFor(comando => comando.SolicitacaoRegulacaoId).NotEmpty().WithMessage("Solicitacao e obrigatoria.");
    }
}

/// <summary>Handler da autorizacao de solicitacao de regulacao.</summary>
public sealed class AutorizarSolicitacaoRegulacaoHandler(
    ISolicitacaoRegulacaoRepository solicitacoes,
    ISisregGateway sisreg,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AutorizarSolicitacaoRegulacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AutorizarSolicitacaoRegulacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var solicitacao = await solicitacoes
            .ObterPorIdAsync(new SolicitacaoRegulacaoId(request.SolicitacaoRegulacaoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Solicitacao nao encontrada.");

        // I-3: guarda de analise + disponibilidade de cota antes de reservar a vaga (evita reserva orfa no SISREG).
        if (solicitacao.Situacao is not (SituacaoSolicitacaoRegulacao.Solicitada or SituacaoSolicitacaoRegulacao.Devolvida))
        {
            throw new InvalidOperationException($"A solicitacao nao esta em analise. Situacao atual: {solicitacao.Situacao}.");
        }

        if (!solicitacao.Cota.TemDisponibilidade())
        {
            throw new InvalidOperationException("Cota esgotada: nao ha vaga disponivel para autorizar.");
        }

        // ACL SISREG (idempotente por SolicitacaoRegulacaoId, com Polly na Infra).
        var protocolo = await sisreg
            .ReservarVagaAsync(solicitacao.Id, solicitacao.Procedimento, cancellationToken)
            .ConfigureAwait(false);

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        solicitacao.Autorizar(protocolo, hoje);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
