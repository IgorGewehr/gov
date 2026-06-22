using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Folha aberta para uma competencia (I-14).</summary>
/// <param name="FolhaDePagamentoId">Identificador da folha.</param>
/// <param name="Competencia">Competencia de referencia.</param>
public sealed record FolhaAberta(FolhaDePagamentoId FolhaDePagamentoId, Competencia Competencia) : IDomainEvent;

/// <summary>Folha calculada (proventos/descontos/liquido apurados — I-5).</summary>
/// <param name="FolhaDePagamentoId">Identificador da folha.</param>
/// <param name="Competencia">Competencia de referencia.</param>
/// <param name="TotalLiquido">Total liquido apurado.</param>
public sealed record FolhaCalculada(FolhaDePagamentoId FolhaDePagamentoId, Competencia Competencia, decimal TotalLiquido) : IDomainEvent;

/// <summary>Folha fechada (dispara S-1299, S-1210, totalizadores e DCTFWeb — I-8).</summary>
/// <param name="FolhaDePagamentoId">Identificador da folha.</param>
/// <param name="Competencia">Competencia de referencia.</param>
public sealed record FolhaFechada(FolhaDePagamentoId FolhaDePagamentoId, Competencia Competencia) : IDomainEvent;

/// <summary>Pagamento do liquido efetuado (liquidacao financeira — S-1210 / I-9).</summary>
/// <param name="FolhaDePagamentoId">Identificador da folha.</param>
/// <param name="Competencia">Competencia de referencia.</param>
/// <param name="DataPagamento">Data do pagamento.</param>
public sealed record PagamentoEfetuado(FolhaDePagamentoId FolhaDePagamentoId, Competencia Competencia, DateOnly DataPagamento) : IDomainEvent;
