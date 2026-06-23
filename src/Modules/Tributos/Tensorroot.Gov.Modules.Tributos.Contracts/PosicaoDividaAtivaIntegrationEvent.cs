using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Tributos.Contracts;

/// <summary>
/// Evento de integração público: a <b>posição consolidada da Dívida Ativa</b> de um exercício foi
/// apurada no módulo Tributos (saldo inscrito + ajuizado + recuperado no período). Complementa
/// <see cref="ReceitaArrecadadaIntegrationEvent"/> (arrecadação) para o KPI de <b>arrecadação tributária
/// + dívida ativa</b> do Painel do Gestor (BI). Idempotente por <c>(TenantId, Exercicio)</c> no
/// consumidor — cada apuração substitui a posição vigente do exercício.
/// <para>
/// Adição retrocompatível ao contrato de Tributos (M8 — Painel do Gestor).
/// </para>
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="Exercicio">Exercício (ano) de referência da posição.</param>
/// <param name="SaldoInscrito">Saldo total inscrito em dívida ativa (estoque) ao fim do período.</param>
/// <param name="SaldoAjuizado">Parcela do estoque já ajuizada (execução fiscal em curso).</param>
/// <param name="RecuperadoNoExercicio">Valor recuperado (recebido) de dívida ativa no exercício.</param>
public sealed record PosicaoDividaAtivaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    int Exercicio,
    decimal SaldoInscrito,
    decimal SaldoAjuizado,
    decimal RecuperadoNoExercicio) : IntegrationEvent(EventId, OccurredOnUtc);
