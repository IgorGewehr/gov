using System.Globalization;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

/// <summary>Resumo de uma Dívida Ativa para leitura.</summary>
/// <param name="Id">Identificador da dívida.</param>
/// <param name="ContribuinteId">Contribuinte devedor.</param>
/// <param name="ValorOriginario">Valor originário inscrito.</param>
/// <param name="Situacao">Situação atual.</param>
/// <param name="DataInscricao">Data de inscrição.</param>
/// <param name="DataPrescricao">Data-limite de prescrição.</param>
/// <param name="NumeroCda">Número da CDA, se emitida.</param>
/// <param name="NumeroInscricao">Número da inscrição no Registro de Dívida Ativa.</param>
public sealed record DividaAtivaResumo(
    Guid Id,
    Guid ContribuinteId,
    decimal ValorOriginario,
    string Situacao,
    DateOnly DataInscricao,
    DateOnly DataPrescricao,
    string? NumeroCda,
    long NumeroInscricao);

/// <summary>Lista as dívidas ativas de um contribuinte.</summary>
/// <param name="ContribuinteId">Contribuinte.</param>
public sealed record ObterDividasAtivasDoContribuinteQuery(Guid ContribuinteId)
    : IQuery<IReadOnlyList<DividaAtivaResumo>>;

/// <summary>Handler da consulta de dívidas ativas do contribuinte.</summary>
public sealed class ObterDividasAtivasDoContribuinteHandler(IDividaAtivaRepository dividas)
    : IQueryHandler<ObterDividasAtivasDoContribuinteQuery, IReadOnlyList<DividaAtivaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DividaAtivaResumo>> Handle(
        ObterDividasAtivasDoContribuinteQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontradas = await dividas
            .ListarPorContribuinteAsync(new ContribuinteId(request.ContribuinteId), cancellationToken)
            .ConfigureAwait(false);

        return encontradas
            .Select(divida => new DividaAtivaResumo(
                divida.Id.Value,
                divida.ContribuinteId.Value,
                divida.ValorOriginario.Valor,
                divida.Situacao.ToString(),
                divida.DataInscricao,
                divida.DataPrescricao,
                divida.NumeroCda,
                divida.NumeroInscricao))
            .ToList();
    }
}
