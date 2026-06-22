using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Contracts;

/// <summary>
/// Evento de integracao publico: um fornecedor recebeu uma sancao administrativa (art. 156),
/// permitindo o bloqueio de habilitacao/contratacao nos agregados Licitacao/Contrato e a
/// transparencia ativa (LAI). Consumivel por outros Bounded Contexts. Idempotente por
/// <c>EventId</c> no consumidor (reentrega via Outbox).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="FornecedorId">Identificador do fornecedor sancionado.</param>
/// <param name="Cnpj">CNPJ do fornecedor (sem mascara).</param>
/// <param name="TipoSancao">Tipo da sancao aplicada (nome do enum).</param>
/// <param name="DataInicio">Inicio da vigencia da sancao.</param>
/// <param name="DataFim">Termo final da vigencia (nulo = sem termo).</param>
public sealed record FornecedorSancionadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid FornecedorId,
    string Cnpj,
    string TipoSancao,
    DateOnly DataInicio,
    DateOnly? DataFim) : IntegrationEvent(EventId, OccurredOnUtc);
