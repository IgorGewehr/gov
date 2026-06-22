using MediatR;
using Microsoft.Extensions.Logging;

namespace Tensorroot.Gov.Modules.Saude.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Administracao): consome <see cref="FornecedorHabilitado"/>
/// para credenciar fornecedores de insumos de saude no Bounded Context Saude. Nao altera o estado do
/// agregado Atendimento (consumo de contexto do modulo). Idempotente por <c>EventId</c> (deduplicacao no Inbox).
/// </summary>
public sealed class ReceberFornecedorHabilitadoHandler(ILogger<ReceberFornecedorHabilitadoHandler> logger)
    : INotificationHandler<FornecedorHabilitado>
{
    /// <inheritdoc />
    public Task Handle(FornecedorHabilitado notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        logger.LogInformation(
            "Fornecedor habilitado recebido da Administracao para tenant {TenantId}: fornecedor {FornecedorId}.",
            notification.TenantId,
            notification.FornecedorId);

        return Task.CompletedTask;
    }
}
