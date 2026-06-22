using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="RemessaTce"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IRemessaTceRepository
{
    /// <summary>Marca uma nova remessa para inserção.</summary>
    /// <param name="remessa">Remessa a adicionar.</param>
    void Adicionar(RemessaTce remessa);

    /// <summary>Obtém uma remessa por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A remessa, ou <c>null</c> se inexistente no tenant.</returns>
    Task<RemessaTce?> ObterPorIdAsync(RemessaTceId id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista as remessas do tenant filtrando por exercício e, opcionalmente, por tipo de período e situação.
    /// </summary>
    /// <param name="exercicio">Ano de exercício.</param>
    /// <param name="tipo">Tipo de período (opcional).</param>
    /// <param name="situacao">Situação (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Remessas do tenant que atendem aos filtros.</returns>
    Task<IReadOnlyList<RemessaTce>> ListarPorPeriodoAsync(
        int exercicio,
        TipoPeriodo? tipo,
        SituacaoRemessaTce? situacao,
        CancellationToken cancellationToken);
}
