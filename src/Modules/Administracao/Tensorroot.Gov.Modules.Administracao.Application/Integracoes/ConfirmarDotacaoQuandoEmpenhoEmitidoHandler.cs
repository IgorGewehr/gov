using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Financas): consome <see cref="EmpenhoEmitidoIntegrationEvent"/>
/// publicado pelo modulo Financas quando um empenho e emitido para o contrato (Lei 4.320; LRF),
/// confirmando a cobertura orcamentaria. Aplica a regra I-8 do Contrato: grava a <c>EmpenhoRef</c>,
/// marca <c>DotacaoConfirmada</c> e, se ja publicado no PNCP, torna o contrato Eficaz.
/// Idempotente por <c>EventId</c> (deduplicacao no Inbox).
/// </summary>
public sealed class ConfirmarDotacaoQuandoEmpenhoEmitidoHandler(
    IContratoRepository contratos,
    IUnitOfWork unitOfWork,
    ILogger<ConfirmarDotacaoQuandoEmpenhoEmitidoHandler> logger)
    : INotificationHandler<EmpenhoEmitidoIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(EmpenhoEmitidoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var contrato = await contratos
            .ObterPorIdAsync(new ContratoId(notification.ContratoId), cancellationToken)
            .ConfigureAwait(false);
        if (contrato is null)
        {
            // Contrato inexistente no tenant (ou ja removido): nada a confirmar.
            logger.LogWarning(
                "Empenho emitido recebido de Financas para contrato {ContratoId} inexistente no tenant {TenantId}; evento {EventId} ignorado.",
                notification.ContratoId,
                notification.TenantId,
                notification.EventId);
            return;
        }

        // I-8: confirma a dotacao gravando a referencia do empenho; idempotente por EventId (Inbox)
        // e tambem por estado (ConfirmarDotacao reescreve EmpenhoRef/DotacaoConfirmada sem duplicar efeito).
        contrato.ConfirmarDotacao(EmpenhoRef.De(notification.EmpenhoId, notification.NumeroEmpenho));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Dotacao confirmada para contrato {ContratoId} (tenant {TenantId}) pelo empenho {NumeroEmpenho}; evento {EventId}.",
            notification.ContratoId,
            notification.TenantId,
            notification.NumeroEmpenho,
            notification.EventId);
    }
}
