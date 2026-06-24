using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>
/// Varre as obras em execução do tenant e emite <see cref="ObraPrazoArt94VencendoIntegrationEvent"/> para
/// os prazos do art. 94 §3 (Lei 14.133/2021) a vencer dentro da janela de antecedência parametrizada
/// (W9.3c). O prazo de assinatura (25 d.u.) aplica-se enquanto a obra não está concluída; o de conclusão
/// (45 d.u.) só após a conclusão. Determinístico: recebe "hoje" do <c>TimeProvider</c> (nunca relógio interno).
/// </summary>
public sealed record VarrerPrazosArt94Command : ICommand<int>;

/// <summary>Handler do varredor de prazos do art. 94 §3.</summary>
public sealed class VarrerPrazosArt94Handler(
    IObraRepository obras,
    IParametrosObraProvider parametros,
    ICalendarioDiasUteis calendario,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<VarrerPrazosArt94Command, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(VarrerPrazosArt94Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var antecedencia = parametros.AntecedenciaAlertaDiasUteis();
        var paramAssinatura = parametros.PrazoAposAssinatura();

        var emExecucao = await obras.ListarEmExecucaoAsync(cancellationToken).ConfigureAwait(false);
        var alertas = 0;

        foreach (var obra in emExecucao)
        {
            // Obra em execução: o prazo relevante é o de publicação/registro após a assinatura (25 d.u.).
            var prazo = obra.CalcularPrazoArt94(TipoPrazoArt94.Assinatura25, paramAssinatura, calendario);
            var restantes = prazo.DiasUteisRestantes(hoje, calendario);

            // Alerta quando o prazo está dentro da janela de antecedência (inclui já vencido).
            if (restantes > antecedencia)
            {
                continue;
            }

            integrationEvents.Enfileirar(new ObraPrazoArt94VencendoIntegrationEvent(
                Guid.NewGuid(),
                timeProvider.GetUtcNow().UtcDateTime,
                tenant.TenantId,
                obra.Id.Value,
                obra.ContratoId,
                TipoPrazoArt94Contrato.Assinatura25,
                prazo.Vencimento,
                restantes));
            alertas++;
        }

        if (alertas > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return alertas;
    }
}
