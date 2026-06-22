using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Contracts;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Gera o autografo de uma proposicao aprovada e o remete ao Executivo (assinado ICP-Brasil).</summary>
/// <param name="ProposicaoId">Proposicao aprovada.</param>
/// <param name="NumeroAutografo">Numero do autografo.</param>
public sealed record GerarAutografoCommand(Guid ProposicaoId, string NumeroAutografo) : ICommand;

/// <summary>Regras de validacao da geracao de autografo.</summary>
public sealed class GerarAutografoValidator : AbstractValidator<GerarAutografoCommand>
{
    /// <summary>Define as regras.</summary>
    public GerarAutografoValidator()
    {
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
        RuleFor(comando => comando.NumeroAutografo).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da geracao de autografo (publica o evento de integracao ao Executivo via Outbox).</summary>
public sealed class GerarAutografoHandler(
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<GerarAutografoCommand>
{
    /// <inheritdoc />
    public async Task Handle(GerarAutografoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        proposicao.GerarAutografo(request.NumeroAutografo, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new AutografoEnviadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            proposicao.Id.Value,
            proposicao.NumeroAutografo!);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
