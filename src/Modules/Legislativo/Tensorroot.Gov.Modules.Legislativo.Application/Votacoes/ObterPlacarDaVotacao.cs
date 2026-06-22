using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

/// <summary>Placar agregado (em tempo real) de uma votacao. Em votacao secreta mostra apenas o agregado (I-12).</summary>
/// <param name="VotacaoId">Identificador da votacao.</param>
/// <param name="Sim">Quantidade de votos Sim.</param>
/// <param name="Nao">Quantidade de votos Nao.</param>
/// <param name="Abstencao">Quantidade de abstencoes.</param>
/// <param name="TotalVotos">Total de votos registrados.</param>
/// <param name="Situacao">Situacao atual.</param>
public sealed record PlacarVotacao(
    Guid VotacaoId,
    int Sim,
    int Nao,
    int Abstencao,
    int TotalVotos,
    string Situacao);

/// <summary>Obtem o placar agregado da votacao (tenant-scoped, em tempo real).</summary>
/// <param name="VotacaoId">Votacao a consultar.</param>
public sealed record ObterPlacarDaVotacaoQuery(Guid VotacaoId) : IQuery<PlacarVotacao>;

/// <summary>Handler da consulta de placar da votacao.</summary>
public sealed class ObterPlacarDaVotacaoHandler(IVotacaoRepository votacoes)
    : IQueryHandler<ObterPlacarDaVotacaoQuery, PlacarVotacao>
{
    /// <inheritdoc />
    public async Task<PlacarVotacao> Handle(ObterPlacarDaVotacaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var votacao = await votacoes.ObterPorIdAsync(new VotacaoId(request.VotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Votacao nao encontrada.");

        return new PlacarVotacao(
            votacao.Id.Value,
            votacao.VotosSim,
            votacao.VotosNao,
            votacao.Abstencoes,
            votacao.Votos.Count,
            votacao.Situacao.ToString());
    }
}
