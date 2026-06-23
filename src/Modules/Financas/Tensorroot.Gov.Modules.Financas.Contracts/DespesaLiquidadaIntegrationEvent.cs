using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Contracts;

/// <summary>
/// Evento de integração público: uma despesa foi <b>liquidada</b> (2º estágio da despesa — Lei 4.320
/// art. 63: verificação do direito adquirido pelo credor). Complementa
/// <see cref="DespesaEmpenhadaIntegrationEvent"/> (empenhado) e <see cref="PagamentoEfetuadoIntegrationEvent"/>
/// (pago), fechando os três estágios que o Painel do Gestor (BI) precisa para a <b>execução
/// orçamentária</b> (empenhado/liquidado/pago vs dotação). Dado aberto (LAI/TCE-RS).
/// <para>
/// Adição retrocompatível ao contrato de Finanças (M8 — Painel do Gestor): produtores antigos
/// continuam válidos; consumidores que ainda não a tratam degradam graciosamente.
/// </para>
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="LiquidacaoId">Identificador da liquidação.</param>
/// <param name="EmpenhoId">Empenho de origem (rastreabilidade do estágio).</param>
/// <param name="Valor">Valor liquidado.</param>
/// <param name="Data">Data da liquidação.</param>
/// <param name="FuncaoSubfuncao">Função + subfunção (FS, 5 dígitos), quando o ciclo orçamentário a carrega. Opcional.</param>
/// <param name="FonteRecurso">Fonte/destinação de recurso (FR), quando disponível. Opcional.</param>
/// <param name="Competencia">Competência (ano/mês) de referência. Opcional.</param>
public sealed record DespesaLiquidadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid LiquidacaoId,
    Guid EmpenhoId,
    decimal Valor,
    DateOnly Data,
    string? FuncaoSubfuncao = null,
    string? FonteRecurso = null,
    DateOnly? Competencia = null) : IntegrationEvent(EventId, OccurredOnUtc);
