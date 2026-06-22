using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Events;

/// <summary>Conta contábil criada no plano de contas.</summary>
/// <param name="ContaId">Identificador da conta.</param>
/// <param name="Codigo">Código PCASP.</param>
public sealed record ContaContabilCriada(ContaContabilId ContaId, string Codigo) : IDomainEvent;

/// <summary>
/// Lançamento contábil registrado (partida dobrada balanceada). Alimenta a projeção do Balancete
/// e a Matriz de Saldos Contábeis (MSC).
/// </summary>
/// <param name="LancamentoId">Identificador do lançamento.</param>
/// <param name="Exercicio">Exercício.</param>
/// <param name="PeriodoMes">Mês (1-12).</param>
public sealed record LancamentoContabilRegistrado(
    LancamentoContabilId LancamentoId,
    int Exercicio,
    int PeriodoMes) : IDomainEvent;

/// <summary>Lançamento contábil estornado (gera lançamento inverso, preserva o original).</summary>
/// <param name="Original">Lançamento original estornado.</param>
/// <param name="Estorno">Lançamento de estorno gerado.</param>
public sealed record LancamentoContabilEstornado(
    LancamentoContabilId Original,
    LancamentoContabilId Estorno) : IDomainEvent;
