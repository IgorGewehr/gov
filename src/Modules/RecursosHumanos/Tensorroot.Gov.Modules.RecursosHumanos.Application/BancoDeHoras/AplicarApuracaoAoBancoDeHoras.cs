using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using DomainBancoDeHoras = Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto.BancoDeHoras;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.BancoDeHoras;

/// <summary>
/// Ao FECHAR uma apuracao de ponto (<see cref="ApuracaoPontoFechada"/>), credita/debita o saldo liquido
/// (extras - faltas) no banco de horas do servidor — o saldo vivo e atualizado a partir do espelho
/// congelado. Idempotente pela apuracao de origem (re-processamento at-least-once do Outbox e no-op).
/// Abre o banco sob demanda na primeira apuracao do servidor. Mantem a ApuracaoPonto desacoplada do
/// banco (gancho via evento, mesma transacao do consumidor).
/// </summary>
public sealed class AplicarApuracaoAoBancoDeHorasHandler(
    IBancoDeHorasRepository bancos,
    ITenantContext tenant,
    IUnitOfWork unitOfWork)
    : INotificationHandler<ApuracaoPontoFechada>
{
    /// <inheritdoc />
    public async Task Handle(ApuracaoPontoFechada notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var saldoLiquido = notification.MinutosExtras - notification.MinutosFalta;
        if (saldoLiquido == 0)
        {
            return; // Nada a lancar.
        }

        var banco = await bancos.ObterPorServidorAsync(notification.ServidorId, cancellationToken).ConfigureAwait(false);
        if (banco is null)
        {
            banco = DomainBancoDeHoras.Abrir(tenant.TenantId, notification.ServidorId);
            bancos.Adicionar(banco);
        }

        // Data-base = primeiro dia da competencia da apuracao (base da prescricao).
        var competencia = new DateOnly(notification.Ano, notification.Mes, 1);
        banco.AplicarApuracao(saldoLiquido, competencia, notification.ApuracaoPontoId.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
