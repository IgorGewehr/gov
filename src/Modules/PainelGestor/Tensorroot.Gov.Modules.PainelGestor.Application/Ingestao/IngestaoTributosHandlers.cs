using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Contracts;

namespace Tensorroot.Gov.Modules.PainelGestor.Application.Ingestao;

/// <summary>
/// ACL de entrada (Tributos → Painel): consome <see cref="ReceitaArrecadadaIntegrationEvent"/> e ACUMULA
/// a arrecadação tributária do exercício. Idempotente por <c>EventId</c> (acumulador — I-13).
/// </summary>
public sealed class ReceberReceitaArrecadadaHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<ReceitaArrecadadaIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(ReceitaArrecadadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, notification.Data.Year, cancellationToken).ConfigureAwait(false);
        snapshot.AcumularArrecadacao(notification.Valor);
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(ReceitaArrecadadaIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// ACL de entrada (Tributos → Painel): consome <see cref="PosicaoDividaAtivaIntegrationEvent"/> e
/// SUBSTITUI a posição da dívida ativa do exercício (estoque inscrito/ajuizado/recuperado).
/// </summary>
public sealed class ReceberPosicaoDividaAtivaHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<PosicaoDividaAtivaIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(PosicaoDividaAtivaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, notification.Exercicio, cancellationToken).ConfigureAwait(false);
        snapshot.DefinirPosicaoDividaAtiva(notification.SaldoInscrito, notification.SaldoAjuizado, notification.RecuperadoNoExercicio);
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(PosicaoDividaAtivaIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
