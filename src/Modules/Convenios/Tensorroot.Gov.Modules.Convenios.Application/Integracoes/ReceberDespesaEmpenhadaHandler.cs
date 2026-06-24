using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.Modules.Financas.Contracts;

namespace Tensorroot.Gov.Modules.Convenios.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Financas): consome <see cref="DespesaEmpenhadaIntegrationEvent"/> para
/// reconhecer o EMPENHO no espelho dos dois fluxos:
/// <list type="bullet">
/// <item>Fluxo A — empenho da CONTRAPARTIDA do convenio recebido (satisfaz A-INV-5, liberando a execucao).</item>
/// <item>Fluxo B — empenho de um REPASSE a OSC (1o estagio do espelho empenho-&gt;liquidacao-&gt;pagamento, B-INV-5).</item>
/// </list>
/// Idempotente por <c>EventId</c> (deduplicacao no Inbox por handler+tenant).
/// <para>
/// // TODO(M10): a CORRELACAO empenho -&gt; convenio/parceria depende de Financas ECOAR o ConvenioId/ParceriaId
/// no evento (hoje o contrato de Financas nao os carrega — ver ContrapartidaConvenioAEmpenhar /
/// RepasseOscAEmpenhar publicados por Convenios). Enquanto o contrato de Financas nao for enriquecido (W2 do
/// M9), este handler registra o reconhecimento; a aplicacao no agregado (RegistrarContrapartidaEmpenhada /
/// VincularExecucaoOrcamentaria) e disparada pelo comando interno correspondente com o vinculo resolvido.
/// </para>
/// </summary>
public sealed class ReceberDespesaEmpenhadaHandler(ILogger<ReceberDespesaEmpenhadaHandler> logger)
    : INotificationHandler<DespesaEmpenhadaIntegrationEvent>
{
    /// <inheritdoc />
    public Task Handle(DespesaEmpenhadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        logger.LogInformation(
            "Despesa empenhada recebida de Financas para tenant {TenantId}: empenho {EmpenhoId} ({Numero}), valor {Valor}. " +
            "Reconhecimento de contrapartida/repasse de Convenios (correlacao por ConvenioId/ParceriaId — // TODO(M10)).",
            notification.TenantId,
            notification.EmpenhoId,
            notification.Numero,
            notification.Valor);

        return Task.CompletedTask;
    }
}
