using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

/// <summary>Projecao de detalhe de uma votacao para leitura.</summary>
/// <param name="Id">Identificador da votacao.</param>
/// <param name="SessaoId">Sessao em que ocorre.</param>
/// <param name="ProposicaoId">Materia (proposicao) votada.</param>
/// <param name="Tipo">Modalidade de apuracao.</param>
/// <param name="MaioriaExigida">Criterio de aprovacao exigido.</param>
/// <param name="TotalMembros">Numero de vereadores da Camara.</param>
/// <param name="Presentes">Numero de presentes.</param>
/// <param name="Turno">Turno da votacao.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="Resultado">Resultado apurado (nulo enquanto aberta).</param>
/// <param name="VotosSim">Quantidade de votos Sim.</param>
/// <param name="VotosNao">Quantidade de votos Nao.</param>
/// <param name="Abstencoes">Quantidade de abstencoes.</param>
public sealed record VotacaoDetalhe(
    Guid Id,
    Guid SessaoId,
    Guid ProposicaoId,
    string Tipo,
    string MaioriaExigida,
    int TotalMembros,
    int Presentes,
    int Turno,
    string Situacao,
    string? Resultado,
    int VotosSim,
    int VotosNao,
    int Abstencoes);

/// <summary>Obtem o detalhe de uma votacao (tenant-scoped). Em votacao secreta nao expoe votos individuais (I-12).</summary>
/// <param name="VotacaoId">Votacao a consultar.</param>
public sealed record ObterVotacaoPorIdQuery(Guid VotacaoId) : IQuery<VotacaoDetalhe>;

/// <summary>Handler da consulta de detalhe da votacao.</summary>
public sealed class ObterVotacaoPorIdHandler(IVotacaoRepository votacoes)
    : IQueryHandler<ObterVotacaoPorIdQuery, VotacaoDetalhe>
{
    /// <inheritdoc />
    public async Task<VotacaoDetalhe> Handle(ObterVotacaoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var votacao = await votacoes.ObterPorIdAsync(new VotacaoId(request.VotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Votacao nao encontrada.");

        return new VotacaoDetalhe(
            votacao.Id.Value,
            votacao.SessaoId.Value,
            votacao.ProposicaoId.Value,
            votacao.Tipo.ToString(),
            votacao.MaioriaExigida.ToString(),
            votacao.TotalMembros,
            votacao.Presentes,
            votacao.Turno,
            votacao.Situacao.ToString(),
            votacao.Resultado?.ToString(),
            votacao.VotosSim,
            votacao.VotosNao,
            votacao.Abstencoes);
    }
}
