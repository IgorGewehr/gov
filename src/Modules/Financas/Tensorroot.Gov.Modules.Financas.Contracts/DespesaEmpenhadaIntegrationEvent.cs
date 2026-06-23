using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Contracts;

/// <summary>
/// Evento de integração público: uma despesa foi empenhada. Empenhos são dado aberto
/// obrigatório — consumível por Transparência (LAI / TCE-RS).
/// <para>
/// <b>M7.0.0 (Via A1 — enriquecimento retrocompatível do contrato).</b> Além da
/// <see cref="ClassificacaoOrcamentaria"/> textual (compatibilidade), o contrato passa a expor os
/// campos <b>decompostos</b> que o eixo fiscal do M7 precisa para classificar a despesa por
/// <b>função</b> (Saúde=10, Educação=12) e por <b>fonte de recurso</b>: <see cref="FuncaoSubfuncao"/>,
/// <see cref="FonteRecurso"/>, <see cref="NaturezaDespesa"/> e <see cref="Competencia"/>. São
/// <b>opcionais</b> (nulos quando o ciclo orçamentário ainda não os carrega — M3.x/M4): produtores
/// antigos continuam válidos e consumidores novos degradam graciosamente. Enquanto o produtor de
/// empenho não os preenche, o read model setorial deriva a classificação da <c>MSCGeradaIntegrationEvent</c>
/// (que já carrega FR/ND/FS por conta) — ver <c>IExecucaoSetorialReadModel</c> no M7.
/// </para>
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="EmpenhoId">Identificador do empenho.</param>
/// <param name="Numero">Número do empenho.</param>
/// <param name="ClassificacaoOrcamentaria">Classificação orçamentária resumida (texto concatenado, compat.).</param>
/// <param name="Valor">Valor empenhado.</param>
/// <param name="CredorNome">Nome/razão social do credor.</param>
/// <param name="CredorDocumento">Documento (CPF/CNPJ) do credor, sem máscara.</param>
/// <param name="FuncaoSubfuncao">Função + subfunção (FS, 5 dígitos, ex.: "10301" = Saúde/Atenção Básica). Opcional.</param>
/// <param name="FonteRecurso">Fonte/destinação de recurso (FR, ex.: "0500" recursos vinculados à saúde). Opcional.</param>
/// <param name="NaturezaDespesa">Natureza da despesa (ND, 8 dígitos). Opcional.</param>
/// <param name="Competencia">Competência (ano/mês) de referência da despesa. Opcional.</param>
public sealed record DespesaEmpenhadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid EmpenhoId,
    string Numero,
    string ClassificacaoOrcamentaria,
    decimal Valor,
    string CredorNome,
    string CredorDocumento,
    string? FuncaoSubfuncao = null,
    string? FonteRecurso = null,
    string? NaturezaDespesa = null,
    DateOnly? Competencia = null) : IntegrationEvent(EventId, OccurredOnUtc);
