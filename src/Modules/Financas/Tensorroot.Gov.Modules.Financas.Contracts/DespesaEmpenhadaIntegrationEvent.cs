using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Contracts;

/// <summary>
/// Evento de integração público: uma despesa foi empenhada. Empenhos são dado aberto
/// obrigatório — consumível por Transparência (LAI / TCE-RS).
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="EmpenhoId">Identificador do empenho.</param>
/// <param name="Numero">Número do empenho.</param>
/// <param name="ClassificacaoOrcamentaria">Classificação orçamentária resumida.</param>
/// <param name="Valor">Valor empenhado.</param>
/// <param name="CredorNome">Nome/razão social do credor.</param>
/// <param name="CredorDocumento">Documento (CPF/CNPJ) do credor, sem máscara.</param>
public sealed record DespesaEmpenhadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid EmpenhoId,
    string Numero,
    string ClassificacaoOrcamentaria,
    decimal Valor,
    string CredorNome,
    string CredorDocumento) : IntegrationEvent(EventId, OccurredOnUtc);
