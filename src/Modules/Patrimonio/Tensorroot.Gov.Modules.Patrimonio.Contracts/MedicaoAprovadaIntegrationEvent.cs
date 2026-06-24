using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Contracts;

/// <summary>
/// Evento de integração público: uma medição de obra foi aprovada pela fiscalização (Lei 4.320/1964,
/// art. 63 — verificação do direito do credor). Finanças o consome para gerar a LIQUIDAÇÃO atrelada ao
/// empenho do contrato (correlação por <see cref="ContratoId"/>), fechando o laço
/// medição → liquidação → pagamento. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="ObraId">Identificador da obra.</param>
/// <param name="ContratoId">Contrato NLLC de origem (correlação com o empenho).</param>
/// <param name="MedicaoId">Identificador da medição aprovada.</param>
/// <param name="NumeroMedicao">Número sequencial da medição na obra.</param>
/// <param name="ValorMedido">Valor medido aprovado (base da liquidação).</param>
/// <param name="CompetenciaAno">Ano da competência.</param>
/// <param name="CompetenciaMes">Mês da competência (1–12).</param>
/// <param name="FornecedorId">Contratada/executora (credor da liquidação).</param>
public sealed record MedicaoAprovadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ObraId,
    Guid ContratoId,
    Guid MedicaoId,
    int NumeroMedicao,
    decimal ValorMedido,
    int CompetenciaAno,
    int CompetenciaMes,
    Guid FornecedorId) : IntegrationEvent(EventId, OccurredOnUtc);
