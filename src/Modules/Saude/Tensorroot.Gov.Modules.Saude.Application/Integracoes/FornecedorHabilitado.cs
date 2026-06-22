using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao publicado pelo modulo Administracao quando um fornecedor
/// e habilitado/credenciado (Lei 14.133/2021). O Bounded Context Saude o consome para credenciar
/// fornecedores de insumos de saude (medicamentos/imunobiologicos). Definido localmente como
/// Anti-Corruption Layer enquanto o evento <c>FornecedorHabilitado</c> ainda nao existe no
/// <c>Tensorroot.Gov.Modules.Administracao.Contracts</c>; quando for publicado pela origem, esta
/// definicao passa a residir la e este placeholder e removido. Idempotente por <c>EventId</c> no consumo.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="FornecedorId">Identificador do fornecedor habilitado.</param>
public sealed record FornecedorHabilitado(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid FornecedorId) : IntegrationEvent(EventId, OccurredOnUtc);
