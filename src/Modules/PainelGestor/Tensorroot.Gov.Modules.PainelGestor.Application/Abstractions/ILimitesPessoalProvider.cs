using Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

namespace Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;

/// <summary>
/// Porta dos limites da Despesa com Pessoal (LRF) VIGENTES por tenant+exercício. Os percentuais NUNCA
/// são hardcoded no cálculo (CLAUDE.md §7): este provedor entrega os limites parametrizados por
/// tenant+vigência (com fallback no padrão legal de referência do Executivo municipal).
/// </summary>
public interface ILimitesPessoalProvider
{
    /// <summary>Obtém os limites de pessoal (LRF) vigentes para o exercício do tenant atual.</summary>
    /// <param name="exercicio">Exercício (ano) — âncora de vigência reprodutível (sem relógio).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os limites vigentes (legal/prudencial/alerta).</returns>
    Task<LimitesPessoalLrf> ObterAsync(int exercicio, CancellationToken cancellationToken);
}
