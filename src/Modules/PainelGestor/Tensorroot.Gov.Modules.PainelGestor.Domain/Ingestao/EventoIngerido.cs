using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Ingestao;

/// <summary>
/// Registro de idempotência da ingestão (ACL de entrada do Painel do Gestor): grava o <c>EventId</c> de
/// cada Integration Event já consumido, por tenant. Os indicadores acumuladores (empenhado/liquidado/
/// pago/arrecadação/pessoal) só somam quando o evento é INÉDITO — a reentrega do Outbox (que é
/// at-least-once) não pode contar a mesma despesa duas vezes (I-13). Os indicadores substituíveis não
/// dependem deste ledger (reaplicar é naturalmente idempotente), mas registramos todos para auditoria
/// de ingestão.
/// </summary>
public sealed class EventoIngerido : IMustHaveTenant
{
    private EventoIngerido()
    {
    }

    private EventoIngerido(Guid eventId, Guid tenantId, string tipoEvento, DateTime ingeridoEmUtc)
    {
        EventId = eventId;
        TenantId = tenantId;
        TipoEvento = tipoEvento;
        IngeridoEmUtc = ingeridoEmUtc;
    }

    /// <summary>Identificador do evento de integração consumido (chave de idempotência).</summary>
    public Guid EventId { get; private init; }

    /// <summary>Tenant (ente público) dono do registro. <see cref="IMustHaveTenant"/>.</summary>
    public Guid TenantId { get; private init; }

    /// <summary>Nome do tipo do evento (para auditoria/diagnóstico da ingestão).</summary>
    public string TipoEvento { get; private init; } = string.Empty;

    /// <summary>Momento (UTC) em que o evento foi ingerido pelo Painel.</summary>
    public DateTime IngeridoEmUtc { get; private init; }

    /// <summary>Cria o registro de um evento ingerido.</summary>
    /// <param name="eventId">EventId do Integration Event.</param>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="tipoEvento">Nome do tipo do evento.</param>
    /// <param name="ingeridoEmUtc">Momento (UTC) da ingestão.</param>
    /// <returns>Novo registro de idempotência.</returns>
    public static EventoIngerido Criar(Guid eventId, Guid tenantId, string tipoEvento, DateTime ingeridoEmUtc)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("EventId é obrigatório.", nameof(eventId));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId é obrigatório.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tipoEvento);
        return new EventoIngerido(eventId, tenantId, tipoEvento, ingeridoEmUtc);
    }
}
