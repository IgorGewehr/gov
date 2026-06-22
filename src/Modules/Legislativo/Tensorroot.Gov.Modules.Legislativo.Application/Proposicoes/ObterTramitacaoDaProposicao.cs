using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Obtem a trilha imutavel de fases/pareceres de uma proposicao (transparencia LAI; tenant-scoped).</summary>
/// <param name="ProposicaoId">Proposicao a consultar.</param>
public sealed record ObterTramitacaoDaProposicaoQuery(Guid ProposicaoId) : IQuery<IReadOnlyList<TramitacaoResumo>>;

/// <summary>Handler da consulta de tramitacao da proposicao.</summary>
public sealed class ObterTramitacaoDaProposicaoHandler(IProposicaoRepository proposicoes)
    : IQueryHandler<ObterTramitacaoDaProposicaoQuery, IReadOnlyList<TramitacaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TramitacaoResumo>> Handle(
        ObterTramitacaoDaProposicaoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        return proposicao.Tramitacoes
            .Select(tramitacao => new TramitacaoResumo(
                tramitacao.Id.Value,
                tramitacao.Fase.ToString(),
                tramitacao.Comissao,
                tramitacao.ParecerFavoravel,
                tramitacao.Data))
            .ToList();
    }
}
