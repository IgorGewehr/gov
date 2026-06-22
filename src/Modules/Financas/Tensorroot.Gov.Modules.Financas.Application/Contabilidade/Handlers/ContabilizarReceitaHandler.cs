using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;

/// <summary>
/// Lançamento contábil automático da receita: ao consumir <see cref="ReceitaArrecadadaRegistrada"/>,
/// gera DOIS lançamentos — orçamentário (D Receita a Realizar / C Receita Realizada) e patrimonial
/// (D Caixa / C VPA). Idempotente por <c>ReceitaId</c>.
/// </summary>
public sealed class ContabilizarReceitaHandler(
    MotorContabil motor,
    IUnitOfWork unitOfWork) : INotificationHandler<ReceitaArrecadadaRegistrada>
{
    /// <inheritdoc />
    public async Task Handle(ReceitaArrecadadaRegistrada notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var gerados = await motor.ContabilizarAsync(
            FatoContabil.ReceitaArrecadada,
            ValorMonetario.De(notification.Valor),
            notification.Data,
            notification.ReceitaId.Value,
            cancellationToken).ConfigureAwait(false);

        if (gerados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
