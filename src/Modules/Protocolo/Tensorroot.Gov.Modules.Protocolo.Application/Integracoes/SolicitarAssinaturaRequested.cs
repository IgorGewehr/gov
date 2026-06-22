using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao em que outro modulo solicita a assinatura por criticidade
/// de um documento ja juntado (Lei 14.063/2020 + Decreto 10.543/2020). Definido localmente como
/// Anti-Corruption Layer enquanto o modulo de origem (e seu <c>Contracts</c>) ainda nao foi gerado;
/// ao ser gerado, esta definicao passa a residir no <c>Contracts</c> da origem. Idempotente por
/// <c>EventId</c> no consumo (deduplicacao no Inbox).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="DocumentoId">Documento a assinar.</param>
/// <param name="SignatarioId">Sujeito que assina.</param>
/// <param name="Tipo">Nivel da assinatura solicitada.</param>
public sealed record SolicitarAssinaturaRequested(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid DocumentoId,
    Guid SignatarioId,
    TipoAssinatura Tipo) : IntegrationEvent(EventId, OccurredOnUtc);
