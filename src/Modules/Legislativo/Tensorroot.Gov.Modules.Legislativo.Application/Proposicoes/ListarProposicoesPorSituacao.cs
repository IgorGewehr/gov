using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Resumo de uma proposicao para listagem.</summary>
/// <param name="Id">Identificador da proposicao.</param>
/// <param name="Tipo">Especie da materia.</param>
/// <param name="Ementa">Resumo do objeto.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="DataApresentacao">Data de apresentacao.</param>
public sealed record ProposicaoResumo(
    Guid Id,
    string Tipo,
    string Ementa,
    string Situacao,
    DateOnly DataApresentacao);

/// <summary>Lista as proposicoes do tenant em uma determinada situacao (tenant-scoped).</summary>
/// <param name="Situacao">Situacao a filtrar (<see cref="SituacaoProposicao"/>).</param>
public sealed record ListarProposicoesPorSituacaoQuery(int Situacao) : IQuery<IReadOnlyList<ProposicaoResumo>>;

/// <summary>Handler da listagem de proposicoes por situacao.</summary>
public sealed class ListarProposicoesPorSituacaoHandler(IProposicaoRepository proposicoes)
    : IQueryHandler<ListarProposicoesPorSituacaoQuery, IReadOnlyList<ProposicaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ProposicaoResumo>> Handle(
        ListarProposicoesPorSituacaoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lista = await proposicoes
            .ListarPorSituacaoAsync((SituacaoProposicao)request.Situacao, cancellationToken)
            .ConfigureAwait(false);

        return lista
            .Select(proposicao => new ProposicaoResumo(
                proposicao.Id.Value,
                proposicao.Tipo.ToString(),
                proposicao.Ementa.Valor,
                proposicao.Situacao.ToString(),
                proposicao.DataApresentacao))
            .ToList();
    }
}
