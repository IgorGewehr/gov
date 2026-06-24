using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;

namespace Tensorroot.Gov.Modules.PainelGestor.Application.Ingestao;

/// <summary>
/// ACL de entrada (RH → Painel): consome <see cref="DespesaPessoalApuradaIntegrationEvent"/> e ACUMULA a
/// despesa com pessoal (base LRF) do exercício — numerador do % da RCL (LRF art. 19/20; alerta art. 169
/// CF). O denominador (RCL) vem de Finanças; o cálculo do % e a comparação com limites são feitos na
/// consulta do Painel. Idempotente por <c>EventId</c> (acumulador — I-13).
/// </summary>
public sealed class ReceberDespesaPessoalHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<DespesaPessoalApuradaIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(DespesaPessoalApuradaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, notification.Exercicio, cancellationToken).ConfigureAwait(false);
        snapshot.AcumularDespesaPessoal(notification.MesReferencia, notification.DespesaPessoalBruta);
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(DespesaPessoalApuradaIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
