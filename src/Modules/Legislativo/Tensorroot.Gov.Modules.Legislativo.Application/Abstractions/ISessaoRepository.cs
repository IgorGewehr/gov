using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Sessao"/>.</summary>
public interface ISessaoRepository
{
    /// <summary>Marca uma nova sessao para insercao.</summary>
    /// <param name="sessao">Sessao a adicionar.</param>
    void Adicionar(Sessao sessao);

    /// <summary>Obtem uma sessao por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A sessao, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Sessao?> ObterPorIdAsync(SessaoId id, CancellationToken cancellationToken);

    /// <summary>Lista as sessoes do tenant em uma determinada situacao.</summary>
    /// <param name="situacao">Situacao a filtrar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sessoes do tenant na situacao informada.</returns>
    Task<IReadOnlyList<Sessao>> ListarPorSituacaoAsync(SituacaoSessao situacao, CancellationToken cancellationToken);
}
