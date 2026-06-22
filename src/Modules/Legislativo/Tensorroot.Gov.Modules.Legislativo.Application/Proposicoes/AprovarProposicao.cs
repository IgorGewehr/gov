using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Aprova uma proposicao em Ordem do Dia conforme o resultado de uma votacao.</summary>
/// <param name="ProposicaoId">Proposicao a aprovar.</param>
/// <param name="VotacaoId">Votacao cujo resultado aprova a materia.</param>
public sealed record AprovarProposicaoCommand(Guid ProposicaoId, Guid VotacaoId) : ICommand;

/// <summary>Regras de validacao da aprovacao de proposicao.</summary>
public sealed class AprovarProposicaoValidator : AbstractValidator<AprovarProposicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AprovarProposicaoValidator()
    {
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
        RuleFor(comando => comando.VotacaoId).NotEmpty();
    }
}

/// <summary>Handler da aprovacao de proposicao.</summary>
public sealed class AprovarProposicaoHandler(
    IProposicaoRepository proposicoes,
    IVotacaoRepository votacoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AprovarProposicaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AprovarProposicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        var votacao = await votacoes.ObterPorIdAsync(new VotacaoId(request.VotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Votacao nao encontrada.");

        if (votacao.Resultado != ResultadoVotacao.Aprovado)
        {
            throw new InvalidOperationException("Votacao nao aprovou a materia.");
        }

        var resultado = ResultadoDeliberacao.Aprovada(MapearMaioria(votacao.MaioriaExigida));
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        proposicao.Aprovar(resultado, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static MaioriaProposicao MapearMaioria(MaioriaExigida maioria) => maioria switch
    {
        MaioriaExigida.Absoluta => MaioriaProposicao.Absoluta,
        MaioriaExigida.Qualificada => MaioriaProposicao.Qualificada,
        _ => MaioriaProposicao.Simples,
    };
}
