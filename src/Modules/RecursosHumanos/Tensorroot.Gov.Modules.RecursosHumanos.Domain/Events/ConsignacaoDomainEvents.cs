using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Consignacao averbada (vigente e lancavel na folha) — consumiu margem do balde.</summary>
/// <param name="ContratoConsignacaoId">Contrato averbado.</param>
/// <param name="ServidorId">Servidor consignante.</param>
/// <param name="GrupoMargem">Balde de margem consumido.</param>
/// <param name="ValorParcela">Valor mensal da parcela.</param>
public sealed record ConsignacaoAverbada(
    ContratoConsignacaoId ContratoConsignacaoId,
    ServidorId ServidorId,
    GrupoMargem GrupoMargem,
    decimal ValorParcela) : IDomainEvent;

/// <summary>Consignacao suspensa (deixa de lancar na folha; libera margem temporariamente).</summary>
/// <param name="ContratoConsignacaoId">Contrato suspenso.</param>
/// <param name="ServidorId">Servidor consignante.</param>
public sealed record ConsignacaoSuspensa(
    ContratoConsignacaoId ContratoConsignacaoId,
    ServidorId ServidorId) : IDomainEvent;

/// <summary>Consignacao quitada (todas as parcelas pagas; libera margem definitivamente).</summary>
/// <param name="ContratoConsignacaoId">Contrato quitado.</param>
/// <param name="ServidorId">Servidor consignante.</param>
public sealed record ConsignacaoQuitada(
    ContratoConsignacaoId ContratoConsignacaoId,
    ServidorId ServidorId) : IDomainEvent;

/// <summary>Consignacao cancelada antes da quitacao (libera margem).</summary>
/// <param name="ContratoConsignacaoId">Contrato cancelado.</param>
/// <param name="ServidorId">Servidor consignante.</param>
public sealed record ConsignacaoCancelada(
    ContratoConsignacaoId ContratoConsignacaoId,
    ServidorId ServidorId) : IDomainEvent;
