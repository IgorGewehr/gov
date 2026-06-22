using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Projecao de detalhe de uma proposicao para leitura.</summary>
/// <param name="Id">Identificador da proposicao.</param>
/// <param name="Tipo">Especie da materia.</param>
/// <param name="Ementa">Resumo do objeto.</param>
/// <param name="Autoria">Autoria (iniciativa).</param>
/// <param name="Regime">Regime de tramitacao.</param>
/// <param name="Protocolo">Numero de protocolo no tenant.</param>
/// <param name="DataApresentacao">Data de apresentacao.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="NumeroAutografo">Numero do autografo, quando gerado.</param>
/// <param name="Tramitacoes">Trilha imutavel de fases/pareceres.</param>
public sealed record ProposicaoDetalhe(
    Guid Id,
    string Tipo,
    string Ementa,
    string Autoria,
    string Regime,
    string Protocolo,
    DateOnly DataApresentacao,
    string Situacao,
    string? NumeroAutografo,
    IReadOnlyList<TramitacaoResumo> Tramitacoes);

/// <summary>Obtem o detalhe de uma proposicao (tenant-scoped).</summary>
/// <param name="ProposicaoId">Proposicao a consultar.</param>
public sealed record ObterProposicaoPorIdQuery(Guid ProposicaoId) : IQuery<ProposicaoDetalhe>;

/// <summary>Handler da consulta de detalhe da proposicao.</summary>
public sealed class ObterProposicaoPorIdHandler(IProposicaoRepository proposicoes)
    : IQueryHandler<ObterProposicaoPorIdQuery, ProposicaoDetalhe>
{
    /// <inheritdoc />
    public async Task<ProposicaoDetalhe> Handle(ObterProposicaoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        var tramitacoes = proposicao.Tramitacoes
            .Select(tramitacao => new TramitacaoResumo(
                tramitacao.Id.Value,
                tramitacao.Fase.ToString(),
                tramitacao.Comissao,
                tramitacao.ParecerFavoravel,
                tramitacao.Data))
            .ToList();

        return new ProposicaoDetalhe(
            proposicao.Id.Value,
            proposicao.Tipo.ToString(),
            proposicao.Ementa.Valor,
            proposicao.Autoria.Valor,
            proposicao.Regime.ToString(),
            proposicao.Protocolo,
            proposicao.DataApresentacao,
            proposicao.Situacao.ToString(),
            proposicao.NumeroAutografo,
            tramitacoes);
    }
}
