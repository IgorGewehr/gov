using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Proposicao"/>.</summary>
public interface IProposicaoRepository
{
    /// <summary>Marca uma nova proposicao para insercao.</summary>
    /// <param name="proposicao">Proposicao a adicionar.</param>
    void Adicionar(Proposicao proposicao);

    /// <summary>Obtem uma proposicao por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A proposicao, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Proposicao?> ObterPorIdAsync(ProposicaoId id, CancellationToken cancellationToken);

    /// <summary>Lista as proposicoes do tenant em uma determinada situacao.</summary>
    /// <param name="situacao">Situacao a filtrar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Proposicoes do tenant na situacao informada.</returns>
    Task<IReadOnlyList<Proposicao>> ListarPorSituacaoAsync(SituacaoProposicao situacao, CancellationToken cancellationToken);
}
