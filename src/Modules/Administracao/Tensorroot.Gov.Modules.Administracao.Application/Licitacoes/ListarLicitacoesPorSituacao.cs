using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Resumo de uma licitacao para listagem.</summary>
/// <param name="Id">Identificador da licitacao.</param>
/// <param name="Objeto">Descricao do objeto licitado.</param>
/// <param name="Modalidade">Modalidade do certame.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="ValorEstimado">Valor estimado/orcado.</param>
/// <param name="NumeroEditalPncp">Identificador da contratacao no PNCP, quando publicado.</param>
public sealed record LicitacaoResumo(
    Guid Id,
    string Objeto,
    string Modalidade,
    string Situacao,
    decimal ValorEstimado,
    string? NumeroEditalPncp);

/// <summary>Lista as licitacoes do tenant, opcionalmente filtradas por situacao (tenant-scoped).</summary>
/// <param name="Situacao">Situacao a filtrar; <c>null</c> retorna todas as licitacoes do tenant.</param>
public sealed record ListarLicitacoesPorSituacaoQuery(SituacaoLicitacao? Situacao)
    : IQuery<IReadOnlyList<LicitacaoResumo>>;

/// <summary>Handler da consulta de licitacoes por situacao.</summary>
public sealed class ListarLicitacoesPorSituacaoHandler(ILicitacaoRepository licitacoes)
    : IQueryHandler<ListarLicitacoesPorSituacaoQuery, IReadOnlyList<LicitacaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LicitacaoResumo>> Handle(
        ListarLicitacoesPorSituacaoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var itens = await licitacoes.ListarPorSituacaoAsync(request.Situacao, cancellationToken).ConfigureAwait(false);

        return itens
            .Select(licitacao => new LicitacaoResumo(
                licitacao.Id.Value,
                licitacao.Objeto,
                licitacao.Modalidade.ToString(),
                licitacao.Situacao.ToString(),
                licitacao.ValorEstimado.Valor,
                licitacao.NumeroEditalPncp))
            .ToList();
    }
}
