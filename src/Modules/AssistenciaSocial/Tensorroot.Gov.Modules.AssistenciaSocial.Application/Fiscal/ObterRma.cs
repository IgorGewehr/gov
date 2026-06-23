using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Fiscal;

/// <summary>Linha consolidada do RMA por servico.</summary>
/// <param name="Servico">Servico socioassistencial (PAIF/PAEFI/SCFV).</param>
/// <param name="Quantidade">Quantidade de atendimentos do servico na competencia.</param>
public sealed record LinhaRmaResultado(TipoServico Servico, int Quantidade);

/// <summary>Resultado consolidado do RMA de uma unidade numa competencia.</summary>
/// <param name="RmaId">Identificador do RMA.</param>
/// <param name="UnidadeAtendimentoId">Unidade consolidada.</param>
/// <param name="Competencia">Competencia (ano/mes).</param>
/// <param name="Fechado">Indica se a competencia foi selada.</param>
/// <param name="TotalAtendimentos">Total de atendimentos consolidados.</param>
/// <param name="Linhas">Linhas por servico.</param>
public sealed record RmaResultado(
    Guid RmaId,
    Guid UnidadeAtendimentoId,
    string Competencia,
    bool Fechado,
    int TotalAtendimentos,
    IReadOnlyList<LinhaRmaResultado> Linhas);

/// <summary>A-2: consulta o RMA consolidado de uma unidade numa competencia, tenant-scoped.</summary>
/// <param name="UnidadeAtendimentoId">Unidade (CRAS/CREAS/Centro POP).</param>
/// <param name="Competencia">Competencia (ano/mes).</param>
public sealed record ObterRmaQuery(Guid UnidadeAtendimentoId, Competencia Competencia) : IQuery<RmaResultado?>;

/// <summary>Handler da consulta do RMA.</summary>
public sealed class ObterRmaHandler(IRegistroMensalAtendimentoRepository rmas)
    : IQueryHandler<ObterRmaQuery, RmaResultado?>
{
    /// <inheritdoc />
    public async Task<RmaResultado?> Handle(ObterRmaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rma = await rmas.ObterPorUnidadeCompetenciaAsync(request.UnidadeAtendimentoId, request.Competencia, cancellationToken).ConfigureAwait(false);
        if (rma is null)
        {
            return null;
        }

        var linhas = rma.Linhas
            .Select(l => new LinhaRmaResultado(l.Servico, l.Quantidade))
            .OrderBy(l => l.Servico)
            .ToList();

        return new RmaResultado(
            rma.Id.Value,
            rma.UnidadeAtendimentoId,
            rma.Competencia.ToString(),
            rma.Situacao == SituacaoRma.Fechado,
            rma.TotalAtendimentos,
            linhas);
    }
}
