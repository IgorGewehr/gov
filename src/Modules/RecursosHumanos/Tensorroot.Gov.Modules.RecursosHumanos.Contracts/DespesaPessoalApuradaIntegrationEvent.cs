using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Contracts;

/// <summary>
/// Evento de integração público (ponte RH -&gt; Painel do Gestor): expõe a <b>Despesa Total com Pessoal</b>
/// de uma competência para a aferição do limite da LRF (LC 101/2000 art. 18/19/20) e do alerta do art. 169
/// da CF. Diferente de <see cref="FolhaFechadaIntegrationEvent"/> (que carrega o líquido, para o empenho
/// contábil), este evento carrega o <b>valor bruto que compõe a base da Despesa com Pessoal</b> da LRF —
/// que é o numerador do % da RCL. O denominador (RCL) vem de Finanças
/// (<c>ReceitaCorrenteLiquidaApuradaIntegrationEvent</c>); o cálculo do % e a comparação com os limites
/// (legal/prudencial/alerta) são feitos no Painel do Gestor (BI), nunca aqui. Idempotente por
/// <c>(TenantId, Exercicio, MesReferencia, TipoFolha)</c> no consumidor.
/// <para>
/// Adição retrocompatível ao contrato do RH (M8 — Painel do Gestor).
/// // TODO(validar-oficial): a composição EXATA da "Despesa com Pessoal" da LRF (art. 18 — inclui
/// encargos, exclui indenizações/inativos custeados por fundo próprio etc.; cômputo alterado pela
/// LC 178/2021) deve ser materializada na classificação da folha pelo RH; até o classificador existir,
/// o ente pode informar o total bruto como aproximação — este evento é a superfície de contrato.
/// </para>
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="Exercicio">Exercício (ano) de referência da competência.</param>
/// <param name="MesReferencia">Mês de referência (1-12) da competência.</param>
/// <param name="TipoFolha">Tipo da folha (<c>Mensal</c>/<c>DecimoTerceiro</c>/<c>Ferias</c>/<c>Rescisao</c>).</param>
/// <param name="DespesaPessoalBruta">Valor bruto que compõe a base da Despesa com Pessoal (LRF).</param>
public sealed record DespesaPessoalApuradaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    int Exercicio,
    int MesReferencia,
    string TipoFolha,
    decimal DespesaPessoalBruta) : IntegrationEvent(EventId, OccurredOnUtc);
