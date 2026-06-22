using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Contracts;

/// <summary>
/// Evento de integracao publico: o resultado anual de um diario de classe foi apurado, alimentando
/// a Situacao do Aluno (2a etapa do Censo) da matricula vinculada. Consumivel por Transparencia e
/// demais modulos interessados na movimentacao academica. Idempotente por <c>EventId</c> no
/// consumidor (reentrega via Outbox).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OcorridoEmUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente municipal/rede) dono do registro.</param>
/// <param name="DiarioClasseId">Identificador do diario apurado.</param>
/// <param name="MatriculaId">Matricula vinculada ao diario.</param>
/// <param name="Resultado">Resultado apurado (Aprovado/Reprovado/ReprovadoPorFrequencia).</param>
public sealed record ResultadoApuradoIntegrationEvent(
    Guid EventId,
    DateTime OcorridoEmUtc,
    Guid TenantId,
    Guid DiarioClasseId,
    Guid MatriculaId,
    string Resultado) : IntegrationEvent(EventId, OcorridoEmUtc);
