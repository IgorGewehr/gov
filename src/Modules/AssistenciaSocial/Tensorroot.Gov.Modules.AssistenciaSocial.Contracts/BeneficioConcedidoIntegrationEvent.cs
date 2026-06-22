using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;

/// <summary>
/// Evento de integracao publico: um beneficio socioassistencial foi concedido a uma familia.
/// Pode notificar Financas (provisao de recurso — Lei 4.320) e Transparencia (dados
/// agregados/anonimizados — LAI). O payload expoe apenas identificadores e dados estritamente
/// necessarios — sem dado sensivel identificavel (NIS/CPF/saude) no barramento (Beneficio I-12;
/// README secao 7). Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (municipio) dono do registro.</param>
/// <param name="BeneficioId">Identificador do beneficio concedido.</param>
/// <param name="FamiliaId">Familia beneficiaria.</param>
/// <param name="Tipo">Tipo do beneficio (Bpc/Pbf/Eventual).</param>
/// <param name="Competencia">Competencia de referencia (mm/aaaa).</param>
/// <param name="Valor">Valor concedido (nulo em cesta basica/provisao em especie).</param>
public sealed record BeneficioConcedidoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid BeneficioId,
    Guid FamiliaId,
    string Tipo,
    string Competencia,
    decimal? Valor) : IntegrationEvent(EventId, OccurredOnUtc);
