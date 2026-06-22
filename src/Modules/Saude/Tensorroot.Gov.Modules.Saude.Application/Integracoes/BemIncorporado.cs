using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao publicado pelo modulo Patrimonio quando um bem e incorporado
/// ao acervo. O Bounded Context Saude o consome para vincular equipamento a UBS/estabelecimento. Definido
/// localmente como Anti-Corruption Layer para nao acoplar a Application do Saude ao
/// <c>Tensorroot.Gov.Modules.Patrimonio.Contracts</c> neste estagio da fabrica; o mapeamento do contrato
/// real (<c>BemIncorporadoIntegrationEvent</c> do Patrimonio.Contracts) ocorre na fronteira de Infraestrutura.
/// Idempotente por <c>EventId</c> no consumo.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="BemPatrimonialId">Identificador do bem incorporado.</param>
public sealed record BemIncorporado(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid BemPatrimonialId) : IntegrationEvent(EventId, OccurredOnUtc);
