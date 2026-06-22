using MediatR;
using Microsoft.Extensions.Logging;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Administracao): consome o evento de integração
/// <see cref="ContratoAssinadoIntegrationEvent"/> publicado pelo módulo Administração (via Contracts)
/// quando um contrato é celebrado. A aquisição via licitação dispara a entrada/tombamento do bem no
/// acervo do Patrimônio. Idempotente por <c>EventId</c> (deduplicação no Inbox).
/// </summary>
public sealed class ReceberContratoAssinadoHandler(ILogger<ReceberContratoAssinadoHandler> logger)
    : INotificationHandler<ContratoAssinadoIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(ContratoAssinadoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // ACL: registra a chegada do contrato celebrado para subsidiar a incorporação do bem adquirido
        // (a incorporação efetiva ocorre via IncorporarBemCommand com os dados patrimoniais completos).
        logger.LogInformation(
            "Contrato assinado recebido da Administracao para tenant {TenantId}: contrato {ContratoId}, fornecedor {FornecedorId}, valor {Valor}.",
            notification.TenantId,
            notification.ContratoId,
            notification.FornecedorId,
            notification.Valor);

        return Task.CompletedTask;
    }
}
