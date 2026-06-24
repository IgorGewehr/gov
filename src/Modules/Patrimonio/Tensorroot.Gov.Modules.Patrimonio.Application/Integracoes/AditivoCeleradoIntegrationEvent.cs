using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integração publicado pelo módulo Administração quando um termo aditivo é
/// celebrado (Lei 14.133/2021, art. 125). Patrimônio o consome para reajustar o teto contratado da obra
/// vinculada ao contrato (I-2). Definido localmente como Anti-Corruption Layer (espelha
/// <see cref="ContratoAssinadoIntegrationEvent"/>); idempotente por <c>EventId</c> no consumo. Nome com o
/// typo "Celerado" preservado para casar com <c>Administracao.Contracts.AditivoCeleradoIntegrationEvent</c>.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="ContratoId">Contrato cujo aditivo foi celebrado.</param>
/// <param name="AditivoId">Identificador do aditivo celebrado.</param>
/// <param name="ValorAtual">Valor vigente do contrato após o aditivo (novo teto da obra — I-2).</param>
/// <param name="NovaVigenciaFim">Data-fim de vigência vigente após o aditivo.</param>
public sealed record AditivoCeleradoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ContratoId,
    Guid AditivoId,
    decimal ValorAtual,
    DateOnly NovaVigenciaFim) : IntegrationEvent(EventId, OccurredOnUtc);
