using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Contracts;

/// <summary>
/// Evento de integração público: um pagamento de despesa pública foi efetuado.
/// Consumível por Transparência (LAI / TCE-RS SIAPC-PAD) e Contabilidade.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="OrdemDePagamentoId">Identificador da ordem de pagamento.</param>
/// <param name="Numero">Número da ordem de pagamento.</param>
/// <param name="ValorTotal">Valor total pago.</param>
/// <param name="DataPagamento">Data do pagamento.</param>
// TODO(revisao-contabil): campos exatos exigidos pelo layout SIAPC/PAD do TCE-RS — confirmar
// antes de remessa real (fora deste escopo).
public sealed record PagamentoEfetuadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid OrdemDePagamentoId,
    string Numero,
    decimal ValorTotal,
    DateOnly DataPagamento) : IntegrationEvent(EventId, OccurredOnUtc);
