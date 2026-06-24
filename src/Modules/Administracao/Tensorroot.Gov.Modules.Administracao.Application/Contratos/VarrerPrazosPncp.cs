using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>
/// Varredura periodica (acionada por scheduler/worker) dos prazos de divulgacao no PNCP (Lei 14.133/2021,
/// art. 94): para cada contrato pendente de divulgacao, avalia se o prazo esta a vencer ou vencido e
/// enfileira o Integration Event correspondente (<see cref="PrazoPncpAVencerIntegrationEvent"/> /
/// <see cref="PrazoPncpVencidoIntegrationEvent"/>) para o Portal do Gestor (W9.1.d). Retorna a quantidade
/// de alertas emitidos (telemetria).
/// </summary>
public sealed record VarrerPrazosPncpCommand : ICommand<int>;

/// <summary>
/// Handler do varredor de prazos PNCP. Reproduzivel: o <c>hoje</c> vem do <see cref="TimeProvider"/>
/// (nunca do relogio interno do dominio); a janela de antecedencia vem dos parametros do tenant (sem
/// numero magico — §16). Os alertas viajam pelo OUTBOX (consistencia transacional; entrega cross-module
/// em escopo dedicado por modulo na drenagem). Idempotencia do alerta = por <c>EventId</c> no consumidor.
/// </summary>
public sealed class VarrerPrazosPncpHandler(
    IContratoRepository contratos,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IPncpParametros pncpParametros,
    ICalendarioDiasUteis calendario,
    TimeProvider timeProvider) : ICommandHandler<VarrerPrazosPncpCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(VarrerPrazosPncpCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var agoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        var antecedencia = pncpParametros.AntecedenciaAlertaDiasUteis();

        var pendentes = await contratos.ListarPendentesPublicacaoPncpAsync(cancellationToken).ConfigureAwait(false);

        var alertas = 0;
        foreach (var contrato in pendentes)
        {
            var veredito = contrato.AvaliarPrazoPncp(hoje, antecedencia, calendario);
            if (veredito == AlertaPrazoPncp.Nenhum || contrato.PrazoPublicacaoPncp is not { } prazo)
            {
                continue;
            }

            switch (veredito)
            {
                case AlertaPrazoPncp.Vencido:
                    integrationEvents.Enfileirar(new PrazoPncpVencidoIntegrationEvent(
                        Guid.NewGuid(),
                        agoraUtc,
                        tenant.TenantId,
                        contrato.Id.Value,
                        prazo.DataLimitePublicacao));
                    alertas++;
                    break;

                case AlertaPrazoPncp.AVencer:
                    integrationEvents.Enfileirar(new PrazoPncpAVencerIntegrationEvent(
                        Guid.NewGuid(),
                        agoraUtc,
                        tenant.TenantId,
                        contrato.Id.Value,
                        prazo.DataLimitePublicacao,
                        prazo.Prazo.DiasUteisRestantes(hoje, calendario)));
                    alertas++;
                    break;

                case AlertaPrazoPncp.Nenhum:
                default:
                    break;
            }
        }

        if (alertas > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return alertas;
    }
}
