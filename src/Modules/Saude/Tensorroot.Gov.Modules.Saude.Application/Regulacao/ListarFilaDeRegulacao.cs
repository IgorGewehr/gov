using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Regulacao;

/// <summary>Resumo de uma solicitacao de regulacao para a fila.</summary>
/// <param name="Id">Identificador da solicitacao.</param>
/// <param name="PacienteId">Paciente da solicitacao.</param>
/// <param name="CodigoSigtap">Codigo SIGTAP do procedimento.</param>
/// <param name="Prioridade">Prioridade (texto).</param>
/// <param name="Situacao">Situacao atual (texto).</param>
/// <param name="DataSolicitacao">Data de abertura.</param>
public sealed record SolicitacaoRegulacaoResumo(
    Guid Id,
    Guid PacienteId,
    string CodigoSigtap,
    string Prioridade,
    string Situacao,
    DateOnly DataSolicitacao);

/// <summary>
/// Lista a fila de regulacao (solicitacoes em analise: {<c>Solicitada</c>,<c>Devolvida</c>}),
/// filtrando opcionalmente por procedimento e prioridade. Sempre tenant-scoped.
/// </summary>
/// <param name="CodigoSigtap">Filtro opcional por codigo SIGTAP.</param>
/// <param name="Prioridade">Filtro opcional por prioridade.</param>
public sealed record ListarFilaDeRegulacaoQuery(string? CodigoSigtap, Prioridade? Prioridade)
    : IQuery<IReadOnlyList<SolicitacaoRegulacaoResumo>>;

/// <summary>Handler da consulta da fila de regulacao.</summary>
public sealed class ListarFilaDeRegulacaoHandler(ISolicitacaoRegulacaoRepository solicitacoes)
    : IQueryHandler<ListarFilaDeRegulacaoQuery, IReadOnlyList<SolicitacaoRegulacaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SolicitacaoRegulacaoResumo>> Handle(
        ListarFilaDeRegulacaoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pendentes = await solicitacoes
            .ListarPendentesAsync(request.CodigoSigtap, request.Prioridade, cancellationToken)
            .ConfigureAwait(false);

        return pendentes
            .Select(solicitacao => new SolicitacaoRegulacaoResumo(
                solicitacao.Id.Value,
                solicitacao.PacienteId.Value,
                solicitacao.Procedimento.CodigoSigtap,
                solicitacao.Prioridade.ToString(),
                solicitacao.Situacao.ToString(),
                solicitacao.DataSolicitacao))
            .ToList();
    }
}
