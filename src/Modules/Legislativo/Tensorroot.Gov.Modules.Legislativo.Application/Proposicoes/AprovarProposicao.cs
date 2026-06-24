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
    ILegislativoParametros parametros,
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

        // BUG-1 (CRITICO): a votacao tem de ser DESTA proposicao. Sem este vinculo, clicar no card
        // errado na ordem do dia aprova a materia X com o resultado da votacao de Y, sem erro.
        if (votacao.ProposicaoId.Value != proposicao.Id.Value)
        {
            throw new InvalidOperationException(
                "A votacao informada nao pertence a esta proposicao; aprovacao recusada (vinculo votacao->proposicao).");
        }

        // A votacao precisa estar encerrada/apurada (Resultado != null) antes de aprovar a materia:
        // o placar de uma votacao Aberta ainda muda.
        if (votacao.Situacao != SituacaoVotacao.Encerrada)
        {
            throw new InvalidOperationException(
                $"A votacao precisa estar Encerrada para aprovar a materia. Situacao atual: {votacao.Situacao}.");
        }

        if (votacao.Resultado != ResultadoVotacao.Aprovado)
        {
            throw new InvalidOperationException("Votacao nao aprovou a materia.");
        }

        // BUG-1 (agravante): a maioria que alimenta a proposicao e a EFETIVAMENTE ATINGIDA pelo placar,
        // nunca a meramente exigida. Aprovado => existe maioria atingida (no minimo a exigida).
        var maioriaAtingida = votacao.MaioriaAtingida()
            ?? throw new InvalidOperationException("Votacao aprovada sem maioria atingida apurada (estado inconsistente).");

        var resultado = ResultadoDeliberacao.Aprovada(MapearMaioria(maioriaAtingida));
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // L-1: o TURNO da votacao deixa de ser dado morto — e consumido na aprovacao. Para a Emenda a LOM
        // (CF/88 art. 29, caput) a materia so se aprova apos DOIS turnos favoraveis, com a maioria exigida
        // em cada e observado o intersticio minimo (parametrizavel por tenant) entre eles. O agregado
        // enforca a sequencia, a maioria por turno e o intervalo; aqui apenas fornecemos os parametros.
        var intersticio = parametros.IntersticioEntreTurnos();
        proposicao.Aprovar(resultado, votacao.Turno, intersticio, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static MaioriaProposicao MapearMaioria(MaioriaExigida maioria) => maioria switch
    {
        MaioriaExigida.Absoluta => MaioriaProposicao.Absoluta,
        MaioriaExigida.Qualificada => MaioriaProposicao.Qualificada,
        _ => MaioriaProposicao.Simples,
    };
}
