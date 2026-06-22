using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="TribunaSessao"/>.</summary>
public interface ITribunaSessaoRepository
{
    /// <summary>Marca uma nova tribuna para insercao.</summary>
    /// <param name="tribuna">Tribuna a adicionar.</param>
    void Adicionar(TribunaSessao tribuna);

    /// <summary>Obtem a tribuna por identificador (com inscricoes/pausas), respeitando o tenant.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tribuna, ou <c>null</c>.</returns>
    Task<TribunaSessao?> ObterPorIdAsync(TribunaSessaoId id, CancellationToken cancellationToken);

    /// <summary>Obtem a tribuna de uma sessao (uma por sessao), respeitando o tenant.</summary>
    /// <param name="sessaoId">Sessao.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tribuna da sessao, ou <c>null</c> se ainda nao aberta.</returns>
    Task<TribunaSessao?> ObterPorSessaoAsync(SessaoId sessaoId, CancellationToken cancellationToken);
}
