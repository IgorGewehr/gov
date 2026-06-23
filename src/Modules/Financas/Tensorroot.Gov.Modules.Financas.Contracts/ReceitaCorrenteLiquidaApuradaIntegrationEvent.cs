using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Contracts;

/// <summary>
/// Evento de integração público: a <b>Receita Corrente Líquida (RCL)</b> de um período de apuração foi
/// consolidada (LRF — LC 101/2000 art. 2º, IV; somatório das receitas correntes dos últimos 12 meses
/// deduzidas as exclusões legais). É o <b>denominador</b> do limite de Despesa com Pessoal (LRF art. 19/20)
/// no Painel do Gestor (BI): % da RCL comprometido com pessoal → alerta art. 169 CF / LRF.
/// Idempotente por <c>(TenantId, Exercicio, MesReferencia)</c> no consumidor.
/// <para>
/// Adição retrocompatível ao contrato de Finanças (M8 — Painel do Gestor). Quem apura a RCL é Finanças
/// (tem a contabilidade/receitas); o Painel apenas a consome para o cálculo do % da RCL.
/// // TODO(validar-oficial): exclusões legais exatas da RCL para o ente municipal (LC 101 art. 2º, IV
/// e §§; deduções de contribuição previdenciária e compensações) — confirmar com o módulo Finanças.
/// </para>
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="Exercicio">Exercício (ano) de referência da apuração.</param>
/// <param name="MesReferencia">Mês de referência (1-12) ao qual se ancora a janela de 12 meses.</param>
/// <param name="ValorRcl">Valor da RCL apurada (12 meses) — sempre &gt; 0 para o cálculo do percentual.</param>
public sealed record ReceitaCorrenteLiquidaApuradaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    int Exercicio,
    int MesReferencia,
    decimal ValorRcl) : IntegrationEvent(EventId, OccurredOnUtc);
