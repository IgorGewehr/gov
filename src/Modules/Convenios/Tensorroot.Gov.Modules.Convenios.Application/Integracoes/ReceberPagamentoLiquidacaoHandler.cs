using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.Modules.Financas.Contracts;

namespace Tensorroot.Gov.Modules.Convenios.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Financas): consome <see cref="DespesaLiquidadaIntegrationEvent"/> para
/// preencher o estagio de LIQUIDACAO do espelho de execucao orcamentaria do repasse a OSC (fluxo B, B-INV-5)
/// e reconciliar a relacao de pagamentos das PCs. Idempotente por <c>EventId</c>.
/// <para>
/// // TODO(M10): correlacao liquidacao -&gt; parceria/repasse depende de Financas ecoar ParceriaId/NumeroParcela
/// (ver RepasseOscAEmpenhar). Por ora, registra o reconhecimento; o vinculo no agregado e feito via
/// VincularExecucaoRepasse.
/// </para>
/// </summary>
public sealed class ReceberDespesaLiquidadaHandler(ILogger<ReceberDespesaLiquidadaHandler> logger)
    : INotificationHandler<DespesaLiquidadaIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(DespesaLiquidadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        logger.LogInformation(
            "Despesa liquidada recebida de Financas para tenant {TenantId}: liquidacao {LiquidacaoId} do empenho {EmpenhoId}, valor {Valor}.",
            notification.TenantId,
            notification.LiquidacaoId,
            notification.EmpenhoId,
            notification.Valor);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Anti-Corruption Layer (entrada — Financas): consome <see cref="PagamentoEfetuadoIntegrationEvent"/> para
/// preencher o estagio de PAGAMENTO do espelho do repasse a OSC (fluxo B, B-INV-5) e reconciliar a
/// <c>relacaoPagamentos</c> das PCs (fluxo A). Idempotente por <c>EventId</c>.
/// <para>
/// // TODO(M10): correlacao pagamento -&gt; convenio/parceria depende de Financas ecoar o vinculo. Por ora,
/// registra o reconhecimento; o vinculo no agregado e feito via VincularExecucaoRepasse.
/// </para>
/// </summary>
public sealed class ReceberPagamentoEfetuadoHandler(ILogger<ReceberPagamentoEfetuadoHandler> logger)
    : INotificationHandler<PagamentoEfetuadoIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(PagamentoEfetuadoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        logger.LogInformation(
            "Pagamento efetuado recebido de Financas para tenant {TenantId}: ordem {OrdemId} ({Numero}), valor {Valor} em {Data}.",
            notification.TenantId,
            notification.OrdemDePagamentoId,
            notification.Numero,
            notification.ValorTotal,
            notification.DataPagamento);
        return Task.CompletedTask;
    }
}
