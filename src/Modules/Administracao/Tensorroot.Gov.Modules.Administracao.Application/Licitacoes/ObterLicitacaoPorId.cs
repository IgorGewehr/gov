using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Resumo de um lote para a projecao de detalhe.</summary>
/// <param name="LoteId">Identificador do lote.</param>
/// <param name="Numero">Numero do lote.</param>
/// <param name="Descricao">Descricao do objeto do lote.</param>
/// <param name="ValorEstimado">Valor estimado do lote.</param>
public sealed record LoteResumo(Guid LoteId, int Numero, string Descricao, decimal ValorEstimado);

/// <summary>Resumo de uma proposta para a projecao de detalhe.</summary>
/// <param name="PropostaId">Identificador da proposta.</param>
/// <param name="FornecedorId">Licitante proponente.</param>
/// <param name="LoteId">Lote disputado.</param>
/// <param name="Valor">Valor ofertado.</param>
/// <param name="Classificacao">Classificacao na disputa.</param>
/// <param name="Situacao">Situacao da proposta.</param>
public sealed record PropostaResumo(
    Guid PropostaId,
    Guid FornecedorId,
    Guid LoteId,
    decimal Valor,
    int? Classificacao,
    string Situacao);

/// <summary>Projecao de detalhe de uma licitacao para leitura.</summary>
/// <param name="Id">Identificador da licitacao.</param>
/// <param name="Objeto">Descricao do objeto licitado.</param>
/// <param name="Modalidade">Modalidade do certame.</param>
/// <param name="CriterioJulgamento">Criterio de julgamento.</param>
/// <param name="ValorEstimado">Valor estimado/orcado.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="NumeroEditalPncp">Identificador da contratacao no PNCP, quando publicado.</param>
/// <param name="PropostaVencedoraId">Proposta vencedora indicada, se houver.</param>
/// <param name="Lotes">Lotes do certame.</param>
/// <param name="Propostas">Propostas recebidas.</param>
public sealed record LicitacaoDetalhe(
    Guid Id,
    string Objeto,
    string Modalidade,
    string CriterioJulgamento,
    decimal ValorEstimado,
    string Situacao,
    string? NumeroEditalPncp,
    Guid? PropostaVencedoraId,
    IReadOnlyList<LoteResumo> Lotes,
    IReadOnlyList<PropostaResumo> Propostas);

/// <summary>Obtem o detalhe de uma licitacao (tenant-scoped).</summary>
/// <param name="LicitacaoId">Licitacao a consultar.</param>
public sealed record ObterLicitacaoPorIdQuery(Guid LicitacaoId) : IQuery<LicitacaoDetalhe?>;

/// <summary>Handler da consulta de detalhe da licitacao.</summary>
public sealed class ObterLicitacaoPorIdHandler(ILicitacaoRepository licitacoes)
    : IQueryHandler<ObterLicitacaoPorIdQuery, LicitacaoDetalhe?>
{
    /// <inheritdoc />
    public async Task<LicitacaoDetalhe?> Handle(ObterLicitacaoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false);
        if (licitacao is null)
        {
            return null;
        }

        return new LicitacaoDetalhe(
            licitacao.Id.Value,
            licitacao.Objeto,
            licitacao.Modalidade.ToString(),
            licitacao.CriterioJulgamento.ToString(),
            licitacao.ValorEstimado.Valor,
            licitacao.Situacao.ToString(),
            licitacao.NumeroEditalPncp,
            licitacao.PropostaVencedoraId,
            licitacao.Lotes
                .Select(lote => new LoteResumo(lote.Id.Value, lote.Numero, lote.Descricao, lote.ValorEstimado.Valor))
                .ToList(),
            licitacao.Propostas
                .Select(proposta => new PropostaResumo(
                    proposta.Id.Value,
                    proposta.FornecedorId,
                    proposta.LoteId.Value,
                    proposta.Valor.Valor,
                    proposta.Classificacao,
                    proposta.Situacao.ToString()))
                .ToList());
    }
}
