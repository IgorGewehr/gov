using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Domain.Tesouraria.Events;

/// <summary>Movimento financeiro registrado numa conta da tesouraria.</summary>
/// <param name="ContaId">Conta movimentada.</param>
/// <param name="MovimentoId">Identificador do movimento.</param>
/// <param name="Tipo">Natureza do movimento.</param>
/// <param name="Valor">Valor do movimento.</param>
/// <param name="Data">Data do movimento.</param>
public sealed record MovimentoFinanceiroRegistrado(
    ContaFinanceiraId ContaId,
    MovimentoFinanceiroId MovimentoId,
    TipoMovimentoFinanceiro Tipo,
    decimal Valor,
    DateOnly Data) : IDomainEvent;
