using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Contracts;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

/// <summary>Encerra e apura uma votacao conforme a maioria exigida (I-5).</summary>
/// <param name="VotacaoId">Votacao a encerrar.</param>
public sealed record EncerrarVotacaoCommand(Guid VotacaoId) : ICommand<ResultadoVotacao>;

/// <summary>Regras de validacao do encerramento de votacao.</summary>
public sealed class EncerrarVotacaoValidator : AbstractValidator<EncerrarVotacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarVotacaoValidator()
    {
        RuleFor(comando => comando.VotacaoId).NotEmpty();
    }
}

/// <summary>Handler do encerramento de votacao; publica <see cref="ResultadoVotacaoIntegrationEvent"/>.</summary>
public sealed class EncerrarVotacaoHandler(
    IVotacaoRepository votacoes,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    TimeProvider timeProvider)
    : ICommandHandler<EncerrarVotacaoCommand, ResultadoVotacao>
{
    /// <inheritdoc />
    public async Task<ResultadoVotacao> Handle(EncerrarVotacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var votacao = await votacoes.ObterPorIdAsync(new VotacaoId(request.VotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Votacao nao encontrada.");

        var resultado = votacao.Encerrar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new ResultadoVotacaoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            votacao.TenantId,
            votacao.Id.Value,
            votacao.ProposicaoId.Value,
            resultado.ToString());

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return resultado;
    }
}
