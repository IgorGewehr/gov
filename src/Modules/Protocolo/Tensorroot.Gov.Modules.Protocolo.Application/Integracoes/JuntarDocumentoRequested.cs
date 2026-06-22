using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao em que um modulo originador (Licitacoes/RH/Licencas)
/// solicita a juntada de um documento (PDF/A + hash SHA-256) a um processo. Definido localmente como
/// Anti-Corruption Layer enquanto os modulos de origem (e seus <c>Contracts</c>) ainda nao foram
/// gerados; ao serem gerados, esta definicao passa a residir no <c>Contracts</c> da origem.
/// Idempotente por <c>EventId</c> no consumo (deduplicacao no Inbox).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ProcessoId">Processo de destino da juntada.</param>
/// <param name="Hash">Resumo SHA-256 do conteudo (64 caracteres hexadecimais).</param>
/// <param name="Criticidade">Criticidade do ato.</param>
/// <param name="NivelAcesso">Visibilidade do documento.</param>
/// <param name="FormatoPdfA">Conformidade com PDF/A (deve ser <c>true</c>).</param>
public sealed record JuntarDocumentoRequested(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ProcessoId,
    string Hash,
    CriticidadeAto Criticidade,
    NivelDeAcesso NivelAcesso,
    bool FormatoPdfA) : IntegrationEvent(EventId, OccurredOnUtc);
