using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Resumo de um item da Ordem do Dia para projecao de leitura.</summary>
/// <param name="ProposicaoId">Proposicao pautada.</param>
/// <param name="Ordem">Posicao na pauta.</param>
public sealed record ItemOrdemDoDiaResumo(Guid ProposicaoId, int Ordem);

/// <summary>Projecao de detalhe de uma sessao para leitura (tenant-scoped).</summary>
/// <param name="Id">Identificador da sessao.</param>
/// <param name="Tipo">Especie da sessao.</param>
/// <param name="DataHora">Momento agendado/realizado.</param>
/// <param name="TotalMembros">Numero de vereadores (base do quorum).</param>
/// <param name="QuorumInstalacao">Quorum de instalacao (maioria absoluta).</param>
/// <param name="Presentes">Quantidade de presencas registradas.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="OrdemDoDia">Itens pautados na Ordem do Dia.</param>
public sealed record SessaoDetalhe(
    Guid Id,
    string Tipo,
    DateTimeOffset DataHora,
    int TotalMembros,
    int QuorumInstalacao,
    int Presentes,
    string Situacao,
    IReadOnlyList<ItemOrdemDoDiaResumo> OrdemDoDia);

/// <summary>Obtem o detalhe de uma sessao (tenant-scoped).</summary>
/// <param name="SessaoId">Sessao a consultar.</param>
public sealed record ObterSessaoPorIdQuery(Guid SessaoId) : IQuery<SessaoDetalhe>;

/// <summary>Handler da consulta de detalhe da sessao.</summary>
public sealed class ObterSessaoPorIdHandler(ISessaoRepository sessoes)
    : IQueryHandler<ObterSessaoPorIdQuery, SessaoDetalhe>
{
    /// <inheritdoc />
    public async Task<SessaoDetalhe> Handle(ObterSessaoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        var ordemDoDia = sessao.OrdemDoDia
            .Select(item => new ItemOrdemDoDiaResumo(item.ProposicaoId.Value, item.Ordem))
            .ToList();

        return new SessaoDetalhe(
            sessao.Id.Value,
            sessao.Tipo.ToString(),
            sessao.DataHora.Valor,
            sessao.TotalMembros,
            sessao.QuorumInstalacao,
            sessao.Presencas.Count,
            sessao.Situacao.ToString(),
            ordemDoDia);
    }
}
