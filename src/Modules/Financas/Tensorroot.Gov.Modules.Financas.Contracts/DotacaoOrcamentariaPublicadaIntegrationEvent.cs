using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Contracts;

/// <summary>
/// Evento de integração público: a <b>dotação orçamentária autorizada</b> de um exercício foi
/// publicada/atualizada (LOA + créditos adicionais — Lei 4.320). É o <b>denominador</b> da execução
/// orçamentária no Painel do Gestor (BI): "% executado vs dotação atualizada". Idempotente por
/// <c>(TenantId, Exercicio)</c> no consumidor — cada publicação substitui o total vigente do exercício
/// (reprocessar não soma duas vezes).
/// <para>
/// Adição retrocompatível ao contrato de Finanças (M8 — Painel do Gestor).
/// </para>
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="Exercicio">Exercício (ano) de referência.</param>
/// <param name="DotacaoInicial">Dotação inicial (LOA) do exercício.</param>
/// <param name="DotacaoAtualizada">Dotação atualizada (LOA + créditos adicionais) — denominador da execução.</param>
public sealed record DotacaoOrcamentariaPublicadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    int Exercicio,
    decimal DotacaoInicial,
    decimal DotacaoAtualizada) : IntegrationEvent(EventId, OccurredOnUtc);
