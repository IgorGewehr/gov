using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;

/// <summary>
/// Lançamento contábil automático do pagamento: ao consumir <see cref="PagamentoEfetuado"/>, gera DOIS
/// lançamentos — orçamentário (D Liquidado a Pagar / C Liquidado Pago) e patrimonial/financeiro
/// (D Fornecedores / C Caixa). Idempotente por <c>OrdemDePagamentoId</c>.
/// </summary>
public sealed class ContabilizarPagamentoHandler(
    MotorContabil motor,
    IOrdemDePagamentoRepository ordens,
    IUnitOfWork unitOfWork) : INotificationHandler<PagamentoEfetuado>
{
    /// <inheritdoc />
    public async Task Handle(PagamentoEfetuado notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var ordem = await ordens.ObterPorIdAsync(notification.OrdemDePagamentoId, cancellationToken).ConfigureAwait(false);
        if (ordem is null)
        {
            return;
        }

        var gerados = await motor.ContabilizarAsync(
            FatoContabil.PagamentoEfetuado,
            ValorMonetario.De(notification.ValorTotal),
            ordem.DataPagamento,
            notification.OrdemDePagamentoId.Value,
            cancellationToken).ConfigureAwait(false);

        if (gerados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
