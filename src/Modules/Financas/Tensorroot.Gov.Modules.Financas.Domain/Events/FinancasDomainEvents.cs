using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Receitas;
using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Domain.Events;

/// <summary>Dotação orçamentária criada (crédito aprovado na LOA).</summary>
/// <param name="DotacaoId">Identificador da dotação.</param>
/// <param name="Exercicio">Exercício orçamentário.</param>
public sealed record DotacaoCriada(DotacaoOrcamentariaId DotacaoId, int Exercicio) : IDomainEvent;

/// <summary>Crédito suplementar/adicional reforçado.</summary>
/// <param name="DotacaoId">Identificador da dotação.</param>
/// <param name="Valor">Valor reforçado.</param>
public sealed record CreditoReforcado(DotacaoOrcamentariaId DotacaoId, decimal Valor) : IDomainEvent;

/// <summary>Crédito orçamentário anulado.</summary>
/// <param name="DotacaoId">Identificador da dotação.</param>
/// <param name="Valor">Valor anulado.</param>
public sealed record CreditoAnulado(DotacaoOrcamentariaId DotacaoId, decimal Valor) : IDomainEvent;

/// <summary>Despesa empenhada (1º estágio — Lei 4.320/64).</summary>
/// <param name="EmpenhoId">Identificador do empenho.</param>
/// <param name="DotacaoId">Dotação onerada.</param>
/// <param name="Valor">Valor empenhado.</param>
public sealed record EmpenhoEmitido(EmpenhoId EmpenhoId, DotacaoOrcamentariaId DotacaoId, decimal Valor) : IDomainEvent;

/// <summary>Empenho anulado (parcial ou total).</summary>
/// <param name="EmpenhoId">Identificador do empenho.</param>
/// <param name="Valor">Valor anulado.</param>
/// <param name="EventoId">
/// Identificador unico DESTE fato de anulacao. Um mesmo empenho pode ser anulado em parcelas
/// distintas; o <c>EventoId</c> garante que a contabilizacao do estorno (lancamento inverso) seja
/// idempotente por FATO — sem ele, anulacoes parciais sucessivas do mesmo empenho colidiriam na
/// chave de idempotencia (origem + evento) e apenas a primeira geraria lancamento.
/// </param>
public sealed record EmpenhoAnulado(EmpenhoId EmpenhoId, decimal Valor, Guid EventoId) : IDomainEvent;

/// <summary>Despesa liquidada (2º estágio — Lei 4.320/64, art. 63).</summary>
/// <param name="LiquidacaoId">Identificador da liquidação.</param>
/// <param name="EmpenhoId">Empenho vinculado.</param>
/// <param name="Valor">Valor liquidado.</param>
public sealed record DespesaLiquidada(LiquidacaoId LiquidacaoId, EmpenhoId EmpenhoId, decimal Valor) : IDomainEvent;

/// <summary>Liquidação estornada.</summary>
/// <param name="LiquidacaoId">Identificador da liquidação.</param>
/// <param name="EmpenhoId">Empenho vinculado.</param>
/// <param name="Valor">Valor estornado.</param>
public sealed record LiquidacaoEstornada(LiquidacaoId LiquidacaoId, EmpenhoId EmpenhoId, decimal Valor) : IDomainEvent;

/// <summary>Pagamento efetuado (3º estágio — Lei 4.320/64, art. 62/64).</summary>
/// <param name="OrdemDePagamentoId">Identificador da ordem de pagamento.</param>
/// <param name="ValorTotal">Valor total pago.</param>
public sealed record PagamentoEfetuado(OrdemDePagamentoId OrdemDePagamentoId, decimal ValorTotal) : IDomainEvent;

/// <summary>Empenho inscrito em Restos a Pagar no encerramento do exercício.</summary>
/// <param name="EmpenhoId">Empenho de origem.</param>
/// <param name="RestoAPagarId">Resto a pagar gerado.</param>
/// <param name="ExercicioInscricao">Exercício de inscrição.</param>
public sealed record EmpenhoInscritoEmRestosAPagar(
    EmpenhoId EmpenhoId,
    RestoAPagarId RestoAPagarId,
    int ExercicioInscricao) : IDomainEvent;

/// <summary>Resto a pagar liquidado (Não Processado).</summary>
/// <param name="RestoAPagarId">Identificador do resto a pagar.</param>
/// <param name="Valor">Valor liquidado.</param>
public sealed record RestoAPagarLiquidado(RestoAPagarId RestoAPagarId, decimal Valor) : IDomainEvent;

/// <summary>Resto a pagar pago.</summary>
/// <param name="RestoAPagarId">Identificador do resto a pagar.</param>
/// <param name="Valor">Valor pago.</param>
public sealed record RestoAPagarPago(RestoAPagarId RestoAPagarId, decimal Valor) : IDomainEvent;

/// <summary>Receita arrecadada registrada em Finanças (gancho para contabilização patrimonial+orçamentária).</summary>
/// <param name="ReceitaId">Identificador da receita.</param>
/// <param name="Valor">Valor arrecadado.</param>
/// <param name="Data">Data da arrecadação.</param>
public sealed record ReceitaArrecadadaRegistrada(
    ReceitaArrecadadaId ReceitaId,
    decimal Valor,
    DateOnly Data) : IDomainEvent;

/// <summary>Resto a pagar inscrito.</summary>
/// <param name="RestoAPagarId">Identificador do resto a pagar.</param>
/// <param name="EmpenhoId">Empenho de origem.</param>
/// <param name="ExercicioInscricao">Exercício de inscrição.</param>
public sealed record RestoAPagarInscrito(
    RestoAPagarId RestoAPagarId,
    EmpenhoId EmpenhoId,
    int ExercicioInscricao) : IDomainEvent;
