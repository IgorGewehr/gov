using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Financas): consome <see cref="DotacaoIndisponivelIntegrationEvent"/>
/// publicado pelo modulo Financas quando nao ha dotacao/credito orcamentario para o contrato (LRF).
/// Aplica a regra I-8 do Contrato: mantem o contrato sem eficacia (reverte de Eficaz para Assinado se
/// necessario) e impede o inicio da execucao. Idempotente por <c>EventId</c> (deduplicacao no Inbox).
/// </summary>
public sealed class BloquearEficaciaQuandoDotacaoIndisponivelHandler(
    IContratoRepository contratos,
    IUnitOfWork unitOfWork,
    ILogger<BloquearEficaciaQuandoDotacaoIndisponivelHandler> logger)
    : INotificationHandler<DotacaoIndisponivelIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(DotacaoIndisponivelIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var contrato = await contratos
            .ObterPorIdAsync(new ContratoId(notification.ContratoId), cancellationToken)
            .ConfigureAwait(false);
        if (contrato is null)
        {
            // Contrato inexistente no tenant (ou ja removido): nada a bloquear.
            logger.LogWarning(
                "Dotacao indisponivel recebida de Financas para contrato {ContratoId} inexistente no tenant {TenantId}; evento {EventId} ignorado.",
                notification.ContratoId,
                notification.TenantId,
                notification.EventId);
            return;
        }

        // I-8: mantem o contrato sem cobertura orcamentaria; idempotente por EventId (Inbox) e por estado
        // (BloquearEficaciaPorDotacaoIndisponivel e reaplicavel sem efeito duplicado).
        contrato.BloquearEficaciaPorDotacaoIndisponivel();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Eficacia bloqueada para contrato {ContratoId} (tenant {TenantId}) por dotacao indisponivel: {Motivo}; evento {EventId}.",
            notification.ContratoId,
            notification.TenantId,
            notification.Motivo,
            notification.EventId);
    }
}
