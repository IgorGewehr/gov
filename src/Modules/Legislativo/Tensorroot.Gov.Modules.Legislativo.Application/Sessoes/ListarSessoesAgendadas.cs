using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Resumo de uma sessao para listagem.</summary>
/// <param name="Id">Identificador da sessao.</param>
/// <param name="Tipo">Especie da sessao.</param>
/// <param name="DataHora">Momento agendado/realizado.</param>
/// <param name="Situacao">Situacao atual.</param>
public sealed record SessaoResumo(Guid Id, string Tipo, DateTimeOffset DataHora, string Situacao);

/// <summary>Lista as sessoes agendadas do tenant (tenant-scoped).</summary>
public sealed record ListarSessoesAgendadasQuery : IQuery<IReadOnlyList<SessaoResumo>>;

/// <summary>Handler da listagem de sessoes agendadas.</summary>
public sealed class ListarSessoesAgendadasHandler(ISessaoRepository sessoes)
    : IQueryHandler<ListarSessoesAgendadasQuery, IReadOnlyList<SessaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SessaoResumo>> Handle(
        ListarSessoesAgendadasQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var agendadas = await sessoes.ListarPorSituacaoAsync(SituacaoSessao.Agendada, cancellationToken).ConfigureAwait(false);

        return agendadas
            .Select(sessao => new SessaoResumo(
                sessao.Id.Value,
                sessao.Tipo.ToString(),
                sessao.DataHora.Valor,
                sessao.Situacao.ToString()))
            .ToList();
    }
}
