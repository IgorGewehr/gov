using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Events;

/// <summary>Obra aberta a partir de um contrato NLLC (vínculo por <c>ContratoId</c>).</summary>
/// <param name="ObraId">Identificador da obra.</param>
/// <param name="ContratoId">Contrato de origem (Lei 14.133/2021).</param>
/// <param name="ValorContratado">Valor contratado (teto da medição acumulada — I-1).</param>
public sealed record ObraAberta(ObraId ObraId, Guid ContratoId, decimal ValorContratado) : IDomainEvent;

/// <summary>
/// Medição de obra aprovada pelo fiscal — gatilho da liquidação em Finanças (Lei 4.320, art. 63).
/// </summary>
/// <param name="ObraId">Identificador da obra.</param>
/// <param name="ContratoId">Contrato de origem (correlação com o empenho).</param>
/// <param name="MedicaoId">Identificador da medição aprovada.</param>
/// <param name="NumeroMedicao">Número sequencial da medição.</param>
/// <param name="ValorMedido">Valor medido aprovado.</param>
/// <param name="CompetenciaAno">Ano da competência.</param>
/// <param name="CompetenciaMes">Mês da competência.</param>
/// <param name="FornecedorId">Contratada/executora.</param>
public sealed record MedicaoAprovada(
    ObraId ObraId,
    Guid ContratoId,
    MedicaoId MedicaoId,
    int NumeroMedicao,
    decimal ValorMedido,
    int CompetenciaAno,
    int CompetenciaMes,
    Guid FornecedorId) : IDomainEvent;

/// <summary>Obra concluída fisicamente (100%) — habilita a incorporação patrimonial interna (I-12).</summary>
/// <param name="ObraId">Identificador da obra.</param>
/// <param name="ContratoId">Contrato de origem.</param>
/// <param name="ValorFinal">Valor medido acumulado final (base do imobilizado).</param>
/// <param name="DataConclusao">Data de conclusão (base do relógio art. 94 §3 — 45 d.u.).</param>
public sealed record ObraConcluida(ObraId ObraId, Guid ContratoId, decimal ValorFinal, DateOnly DataConclusao) : IDomainEvent;

/// <summary>Obra incorporada ao acervo como bem patrimonial (imobilizado) — I-13.</summary>
/// <param name="ObraId">Identificador da obra.</param>
/// <param name="BemPatrimonialId">Bem patrimonial criado pela incorporação.</param>
/// <param name="ValorFinal">Valor incorporado (custo do ativo).</param>
public sealed record ObraIncorporada(ObraId ObraId, Guid BemPatrimonialId, decimal ValorFinal) : IDomainEvent;
