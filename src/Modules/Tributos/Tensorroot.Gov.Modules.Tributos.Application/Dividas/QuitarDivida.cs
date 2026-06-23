using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Contracts;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

/// <summary>Quita uma Dívida Ativa e publica a receita arrecadada + a nova posição da dívida ativa.</summary>
/// <param name="DividaAtivaId">Dívida ativa a quitar.</param>
public sealed record QuitarDividaCommand(Guid DividaAtivaId) : ICommand;

/// <summary>
/// Handler da quitação de Dívida Ativa. Enfileira no Outbox (consistência transacional — padrão
/// FecharFolha/GerarMsc, H5-safe no despacho isolado por mensagem) o
/// <see cref="ReceitaArrecadadaIntegrationEvent"/> (arrecadação — consumido por Finanças e pelo Painel do
/// Gestor) e o <see cref="PosicaoDividaAtivaIntegrationEvent"/> (estoque inscrito/ajuizado/recuperado do
/// exercício mudou com a quitação — KPI de dívida ativa no Painel). Ambos na MESMA transação da quitação.
/// </summary>
public sealed class QuitarDividaHandler(
    IDividaAtivaRepository dividas,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter integrationEvents,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<QuitarDividaCommand>
{
    /// <inheritdoc />
    public async Task Handle(QuitarDividaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var divida = await dividas.ObterPorIdAsync(new DividaAtivaId(request.DividaAtivaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dívida ativa não encontrada.");

        divida.Quitar();

        var agoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        var exercicio = divida.DataInscricao.Year;

        // 1) Receita arrecadada (dado aberto + KPI de arrecadação). Enfileirado no Outbox na transação.
        integrationEvents.Enfileirar(new ReceitaArrecadadaIntegrationEvent(
            Guid.NewGuid(),
            agoraUtc,
            tenant.TenantId,
            request.DividaAtivaId,
            divida.ValorOriginario.Valor,
            DateOnly.FromDateTime(agoraUtc)));

        // 2) Posição consolidada da dívida ativa do exercício APÓS a quitação (a quitação já está no change
        // tracker; a projeção lê o estado vigente). Substitui no consumidor por (Tenant, Exercicio).
        var posicao = await dividas.ObterPosicaoPorExercicioAsync(exercicio, cancellationToken).ConfigureAwait(false);
        integrationEvents.Enfileirar(new PosicaoDividaAtivaIntegrationEvent(
            Guid.NewGuid(),
            agoraUtc,
            tenant.TenantId,
            exercicio,
            posicao.SaldoInscrito,
            posicao.SaldoAjuizado,
            posicao.RecuperadoNoExercicio));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
