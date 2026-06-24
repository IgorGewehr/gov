using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Contracts;

/// <summary>
/// Tipo do prazo do art. 94 §3 (Lei 14.133/2021) sinalizado ao Portal do Gestor.
/// </summary>
public enum TipoPrazoArt94Contrato
{
    /// <summary>Publicação/registro após a assinatura do contrato (padrão 25 dias úteis).</summary>
    Assinatura25 = 1,

    /// <summary>Publicação/registro após a conclusão da obra (padrão 45 dias úteis).</summary>
    Conclusao45 = 2,
}

/// <summary>
/// Evento de integração público: alerta preventivo de prazo do art. 94 §3 (Lei 14.133/2021) a vencer
/// para uma obra (espelha o <c>PrazoPncpAVencer</c> de W9.1). Consumido pelo Portal do Gestor.
/// Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="ObraId">Identificador da obra.</param>
/// <param name="ContratoId">Contrato NLLC de origem.</param>
/// <param name="TipoPrazo">Tipo do prazo (assinatura/conclusão).</param>
/// <param name="DataLimite">Data-limite resolvida do prazo (vencimento).</param>
/// <param name="DiasUteisRestantes">Dias úteis restantes até o vencimento (negativo se já vencido).</param>
public sealed record ObraPrazoArt94VencendoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ObraId,
    Guid ContratoId,
    TipoPrazoArt94Contrato TipoPrazo,
    DateOnly DataLimite,
    int DiasUteisRestantes) : IntegrationEvent(EventId, OccurredOnUtc);
