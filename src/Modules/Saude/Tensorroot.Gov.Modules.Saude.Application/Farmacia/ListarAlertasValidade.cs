using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Saude.Application.Farmacia;

/// <summary>
/// Lista lotes a vencer (ou vencidos) ate uma data-horizonte — alerta de validade para remanejamento/
/// descarte. Dado operacional (sem paciente) — sem trilha LGPD. Tenant-scoped.
/// </summary>
/// <param name="DiasHorizonte">Janela em dias a frente para considerar "a vencer" (default 30).</param>
public sealed record ListarAlertasValidadeQuery(int? DiasHorizonte)
    : IQuery<IReadOnlyList<AlertaValidadeDto>>;

/// <summary>Handler do alerta de validade.</summary>
public sealed class ListarAlertasValidadeHandler(
    IEstoqueMedicamentoRepository estoques,
    IMedicamentoRepository medicamentos,
    TimeProvider timeProvider)
    : IQueryHandler<ListarAlertasValidadeQuery, IReadOnlyList<AlertaValidadeDto>>
{
    private const int HorizontePadrao = 30;

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertaValidadeDto>> Handle(
        ListarAlertasValidadeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var dias = request.DiasHorizonte is { } d && d > 0 ? d : HorizontePadrao;
        var limite = hoje.AddDays(dias);

        var estoquesComAlerta = await estoques.ListarComLotesAVencerAsync(limite, cancellationToken).ConfigureAwait(false);

        var alertas = new List<AlertaValidadeDto>();
        foreach (var estoque in estoquesComAlerta)
        {
            var medicamento = await medicamentos.ObterPorIdAsync(estoque.MedicamentoId, cancellationToken).ConfigureAwait(false);
            foreach (var lote in estoque.Lotes.Where(l => l.Saldo > 0m && l.Validade <= limite).OrderBy(l => l.Validade))
            {
                alertas.Add(new AlertaValidadeDto(
                    estoque.EstabelecimentoId.Value,
                    estoque.MedicamentoId.Value,
                    medicamento?.PrincipioAtivo ?? string.Empty,
                    lote.NumeroLote,
                    lote.Validade,
                    lote.Saldo,
                    lote.EstaVencido(hoje)));
            }
        }

        return alertas;
    }
}
