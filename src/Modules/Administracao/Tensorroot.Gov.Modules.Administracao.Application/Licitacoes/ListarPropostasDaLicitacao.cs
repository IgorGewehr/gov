using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Lista as propostas de uma licitacao (tenant-scoped).</summary>
/// <param name="LicitacaoId">Licitacao a consultar.</param>
public sealed record ListarPropostasDaLicitacaoQuery(Guid LicitacaoId)
    : IQuery<IReadOnlyList<PropostaResumo>>;

/// <summary>Handler da consulta de propostas da licitacao.</summary>
public sealed class ListarPropostasDaLicitacaoHandler(ILicitacaoRepository licitacoes)
    : IQueryHandler<ListarPropostasDaLicitacaoQuery, IReadOnlyList<PropostaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PropostaResumo>> Handle(
        ListarPropostasDaLicitacaoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        return licitacao.Propostas
            .Select(proposta => new PropostaResumo(
                proposta.Id.Value,
                proposta.FornecedorId,
                proposta.LoteId.Value,
                proposta.Valor.Valor,
                proposta.Classificacao,
                proposta.Situacao.ToString()))
            .ToList();
    }
}
