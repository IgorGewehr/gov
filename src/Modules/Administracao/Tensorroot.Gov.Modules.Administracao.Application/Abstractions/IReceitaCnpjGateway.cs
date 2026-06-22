using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>
/// Anti-Corruption Layer (entrada — Receita Federal): consulta de existencia/situacao cadastral
/// de um CNPJ na criacao do fornecedor (I-2). A implementacao concreta (Infrastructure) e
/// resiliente (Polly: retry + circuit breaker + timeout) e idempotente por CNPJ; a indisponibilidade
/// do servico nao corrompe o agregado (o CNPJ formal ja e validado localmente por <c>Cnpj.Create</c>).
/// </summary>
public interface IReceitaCnpjGateway
{
    /// <summary>Verifica se o CNPJ existe e esta com situacao cadastral ativa na Receita.</summary>
    /// <param name="cnpj">CNPJ formalmente valido a consultar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o CNPJ esta ativo/regular na Receita; caso contrario, <c>false</c>.</returns>
    Task<bool> EstaAtivoAsync(Cnpj cnpj, CancellationToken cancellationToken);
}
