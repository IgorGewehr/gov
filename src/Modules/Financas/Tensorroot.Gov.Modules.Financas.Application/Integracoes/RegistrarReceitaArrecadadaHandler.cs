using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Receitas;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Tributos.Contracts;

namespace Tensorroot.Gov.Modules.Financas.Application.Integracoes;

/// <summary>
/// Consome o evento de integração <see cref="ReceitaArrecadadaIntegrationEvent"/> publicado
/// pelo módulo Tributos (via Contracts) e registra a receita correspondente em Finanças.
/// </summary>
public sealed class RegistrarReceitaArrecadadaHandler(IReceitaArrecadadaRepository receitas, IUnitOfWork unitOfWork)
    : INotificationHandler<ReceitaArrecadadaIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(ReceitaArrecadadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var receita = ReceitaArrecadada.Registrar(
            notification.TenantId,
            notification.OrigemId,
            ValorMonetario.De(notification.Valor),
            notification.Data);

        receitas.Adicionar(receita);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
