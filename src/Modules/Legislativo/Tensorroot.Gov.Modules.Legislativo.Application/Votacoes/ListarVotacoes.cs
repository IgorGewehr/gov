using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

/// <summary>Resumo de uma votacao para a lista do painel (sem becos de consulta-por-ID).</summary>
/// <param name="Id">Identificador da votacao.</param>
/// <param name="SessaoId">Sessao em que ocorre.</param>
/// <param name="ProposicaoId">Materia votada.</param>
/// <param name="Tipo">Modalidade de apuracao.</param>
/// <param name="MaioriaExigida">Criterio de aprovacao exigido.</param>
/// <param name="Turno">Turno (1 ou 2).</param>
/// <param name="Situacao">Situacao atual (Aberta/Encerrada/Cancelada).</param>
/// <param name="Resultado">Resultado apurado (nulo enquanto aberta).</param>
/// <param name="TotalVotos">Total de votos registrados.</param>
public sealed record VotacaoResumo(
    Guid Id,
    Guid SessaoId,
    Guid ProposicaoId,
    string Tipo,
    string MaioriaExigida,
    int Turno,
    string Situacao,
    string? Resultado,
    int TotalVotos);

/// <summary>Lista as votacoes do tenant, opcionalmente filtradas por sessao (abertas primeiro).</summary>
/// <param name="SessaoId">Sessao a filtrar (nulo = todas do tenant).</param>
public sealed record ListarVotacoesQuery(Guid? SessaoId) : IQuery<IReadOnlyList<VotacaoResumo>>;

/// <summary>Handler da listagem de votacoes.</summary>
public sealed class ListarVotacoesHandler(IVotacaoRepository votacoes)
    : IQueryHandler<ListarVotacoesQuery, IReadOnlyList<VotacaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<VotacaoResumo>> Handle(ListarVotacoesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        SessaoId? sessao = request.SessaoId is { } id ? new SessaoId(id) : null;
        var lista = await votacoes.ListarAsync(sessao, cancellationToken).ConfigureAwait(false);

        return lista.Select(votacao => new VotacaoResumo(
            votacao.Id.Value,
            votacao.SessaoId.Value,
            votacao.ProposicaoId.Value,
            votacao.Tipo.ToString(),
            votacao.MaioriaExigida.ToString(),
            votacao.Turno,
            votacao.Situacao.ToString(),
            votacao.Resultado?.ToString(),
            votacao.Votos.Count)).ToList();
    }
}
