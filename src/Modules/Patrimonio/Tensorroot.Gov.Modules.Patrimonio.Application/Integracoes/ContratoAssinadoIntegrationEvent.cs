using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integração publicado pelo módulo Administração quando um contrato é
/// celebrado/assinado (Lei 14.133/2021). Patrimônio o consome para disparar a entrada/tombamento do
/// bem adquirido. Definido localmente como Anti-Corruption Layer enquanto o módulo Administração
/// (e seu <c>Contracts</c>) ainda não foi gerado; ao ser gerado, esta definição passa a residir em
/// <c>Tensorroot.Gov.Modules.Administracao.Contracts</c>. Idempotente por <c>EventId</c> no consumo.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="ContratoId">Identificador do contrato celebrado.</param>
/// <param name="FornecedorId">Identificador do fornecedor contratado.</param>
/// <param name="Valor">Valor global do contrato.</param>
/// <param name="LicitacaoId">Identificador da licitação de origem (quando houver).</param>
public sealed record ContratoAssinadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ContratoId,
    Guid FornecedorId,
    decimal Valor,
    Guid? LicitacaoId) : IntegrationEvent(EventId, OccurredOnUtc);
