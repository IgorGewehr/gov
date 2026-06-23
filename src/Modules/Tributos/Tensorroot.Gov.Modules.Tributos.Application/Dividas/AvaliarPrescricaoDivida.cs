using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

/// <summary>Situação prescricional e valores apurados de uma dívida numa data de referência.</summary>
/// <param name="DividaAtivaId">Dívida avaliada.</param>
/// <param name="TermoInicialPrescricao">Termo inicial efetivo (constituição definitiva ou última interrupção).</param>
/// <param name="DataPrescricao">Data-limite da prescrição (CTN art. 174).</param>
/// <param name="EstaPrescrita">Indica se a dívida está prescrita na data de referência.</param>
/// <param name="ValorOriginario">Valor originário (R$).</param>
/// <param name="CorrecaoMonetaria">Correção monetária apurada na data (R$).</param>
/// <param name="Multa">Multa de mora apurada na data (R$).</param>
/// <param name="Juros">Juros de mora apurados na data (R$).</param>
/// <param name="ValorAtualizado">Valor atualizado total na data (R$).</param>
public sealed record AvaliacaoPrescricaoDivida(
    Guid DividaAtivaId,
    DateOnly TermoInicialPrescricao,
    DateOnly DataPrescricao,
    bool EstaPrescrita,
    decimal ValorOriginario,
    decimal CorrecaoMonetaria,
    decimal Multa,
    decimal Juros,
    decimal ValorAtualizado);

/// <summary>
/// Avalia a prescrição (CTN art. 174) e os encargos de uma dívida numa data de referência informada
/// (sem relógio no cálculo — determinístico). Sinaliza se está prescrita.
/// </summary>
/// <param name="DividaAtivaId">Dívida ativa.</param>
/// <param name="DataReferencia">Data de referência (data do fato — ex.: hoje administrativo).</param>
public sealed record AvaliarPrescricaoDividaQuery(Guid DividaAtivaId, DateOnly DataReferencia)
    : IQuery<AvaliacaoPrescricaoDivida>;

/// <summary>Handler da avaliação de prescrição/encargos.</summary>
public sealed class AvaliarPrescricaoDividaHandler(IDividaAtivaRepository dividas)
    : IQueryHandler<AvaliarPrescricaoDividaQuery, AvaliacaoPrescricaoDivida>
{
    /// <inheritdoc />
    public async Task<AvaliacaoPrescricaoDivida> Handle(AvaliarPrescricaoDividaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var divida = await dividas.ObterPorIdAsync(new DividaAtivaId(request.DividaAtivaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dívida ativa não encontrada.");

        var encargos = divida.ApurarEncargos(request.DataReferencia);

        return new AvaliacaoPrescricaoDivida(
            divida.Id.Value,
            divida.TermoInicialPrescricao,
            divida.DataPrescricao,
            divida.EstaPrescrita(request.DataReferencia),
            encargos.ValorOriginario.Valor,
            encargos.CorrecaoMonetaria.Valor,
            encargos.Multa.Valor,
            encargos.Juros.Valor,
            encargos.ValorAtualizado.Valor);
    }
}
