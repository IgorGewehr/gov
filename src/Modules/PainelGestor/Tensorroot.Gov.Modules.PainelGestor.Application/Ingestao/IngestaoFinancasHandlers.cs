using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;

namespace Tensorroot.Gov.Modules.PainelGestor.Application.Ingestao;

/// <summary>
/// ACL de entrada (Finanças → Painel do Gestor): consome <see cref="DespesaEmpenhadaIntegrationEvent"/>
/// e ACUMULA o empenhado do exercício. Idempotente por <c>EventId</c> (acumulador não pode contar duas
/// vezes na reentrega at-least-once do Outbox — I-13).
/// </summary>
public sealed class ReceberDespesaEmpenhadaHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<DespesaEmpenhadaIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(DespesaEmpenhadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var exercicio = notification.Competencia?.Year ?? notification.OccurredOnUtc.Year;
        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, exercicio, cancellationToken).ConfigureAwait(false);
        snapshot.AcumularEmpenhado(notification.Valor);
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(DespesaEmpenhadaIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// ACL de entrada (Finanças → Painel): consome <see cref="DespesaLiquidadaIntegrationEvent"/> e acumula o
/// liquidado do exercício. Idempotente por <c>EventId</c>.
/// </summary>
public sealed class ReceberDespesaLiquidadaHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<DespesaLiquidadaIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(DespesaLiquidadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var exercicio = notification.Competencia?.Year ?? notification.Data.Year;
        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, exercicio, cancellationToken).ConfigureAwait(false);
        snapshot.AcumularLiquidado(notification.Valor);
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(DespesaLiquidadaIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// ACL de entrada (Finanças → Painel): consome <see cref="PagamentoEfetuadoIntegrationEvent"/> e acumula
/// o pago do exercício. Idempotente por <c>EventId</c>.
/// </summary>
public sealed class ReceberPagamentoEfetuadoHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<PagamentoEfetuadoIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(PagamentoEfetuadoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var exercicio = notification.DataPagamento.Year;
        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, exercicio, cancellationToken).ConfigureAwait(false);
        snapshot.AcumularPago(notification.ValorTotal);
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(PagamentoEfetuadoIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// ACL de entrada (Finanças → Painel): consome <see cref="DotacaoOrcamentariaPublicadaIntegrationEvent"/>
/// e SUBSTITUI a dotação atualizada do exercício (denominador da execução). Reaplicar é naturalmente
/// idempotente (substitui), mas registramos o evento para auditoria de ingestão.
/// </summary>
public sealed class ReceberDotacaoOrcamentariaHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<DotacaoOrcamentariaPublicadaIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(DotacaoOrcamentariaPublicadaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, notification.Exercicio, cancellationToken).ConfigureAwait(false);
        snapshot.DefinirDotacao(notification.DotacaoAtualizada);
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(DotacaoOrcamentariaPublicadaIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// ACL de entrada (Finanças → Painel): consome <see cref="ReceitaCorrenteLiquidaApuradaIntegrationEvent"/>
/// e SUBSTITUI a RCL vigente do exercício (denominador do % da LRF), mantendo o mês mais recente.
/// </summary>
public sealed class ReceberRclApuradaHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<ReceitaCorrenteLiquidaApuradaIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(ReceitaCorrenteLiquidaApuradaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, notification.Exercicio, cancellationToken).ConfigureAwait(false);
        snapshot.DefinirReceitaCorrenteLiquida(notification.ValorRcl, notification.MesReferencia);
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(ReceitaCorrenteLiquidaApuradaIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
