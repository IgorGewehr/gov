using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Licitacao"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface ILicitacaoRepository
{
    /// <summary>Marca uma nova licitacao para insercao.</summary>
    /// <param name="licitacao">Licitacao a adicionar.</param>
    void Adicionar(Licitacao licitacao);

    /// <summary>Obtem uma licitacao por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A licitacao, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Licitacao?> ObterPorIdAsync(LicitacaoId id, CancellationToken cancellationToken);

    /// <summary>Lista as licitacoes do tenant, opcionalmente filtradas por situacao.</summary>
    /// <param name="situacao">Situacao a filtrar; <c>null</c> retorna todas as licitacoes do tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Licitacoes do tenant (todas, ou apenas as da situacao informada).</returns>
    Task<IReadOnlyList<Licitacao>> ListarPorSituacaoAsync(SituacaoLicitacao? situacao, CancellationToken cancellationToken);
}
