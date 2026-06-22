using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Contracts;

/// <summary>
/// Evento de integracao publico: um aluno foi matriculado (Matricula Inicial / Rematricula)
/// no modulo Educacao. Alimenta a Transparencia e os coeficientes do FUNDEB/PNAE/PNATE em
/// Financas/Patrimonio. Consumivel por outros Bounded Contexts. Idempotente por <c>EventId</c>
/// no consumidor (reentrega via Outbox).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente municipal/rede) dono do registro.</param>
/// <param name="MatriculaId">Identificador da matricula criada.</param>
/// <param name="AlunoId">Aluno vinculado.</param>
/// <param name="TurmaId">Turma de enturmacao.</param>
/// <param name="EscolaId">Escola da matricula.</param>
public sealed record AlunoMatriculadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid MatriculaId,
    Guid AlunoId,
    Guid TurmaId,
    Guid EscolaId) : IntegrationEvent(EventId, OccurredOnUtc);
