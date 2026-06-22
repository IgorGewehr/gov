using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Votacao"/>.</summary>
public interface IVotacaoRepository
{
    /// <summary>Marca uma nova votacao para insercao.</summary>
    /// <param name="votacao">Votacao a adicionar.</param>
    void Adicionar(Votacao votacao);

    /// <summary>Obtem uma votacao por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A votacao, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Votacao?> ObterPorIdAsync(VotacaoId id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista as votacoes do tenant, da mais recente para a mais antiga; opcionalmente filtradas por
    /// sessao (alimenta o painel ao vivo e a ponte Sessao → Votacoes, removendo a consulta-por-ID).
    /// </summary>
    /// <param name="sessaoId">Sessao a filtrar (nulo = todas do tenant).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Votacoes do tenant (escopadas pelo filtro global).</returns>
    Task<IReadOnlyList<Votacao>> ListarAsync(SessaoId? sessaoId, CancellationToken cancellationToken);
}
