using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Beneficio"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IBeneficioRepository
{
    /// <summary>Marca um novo beneficio para insercao.</summary>
    /// <param name="beneficio">Beneficio a adicionar.</param>
    void Adicionar(Beneficio beneficio);

    /// <summary>Obtem um beneficio por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O beneficio, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Beneficio?> ObterPorIdAsync(BeneficioId id, CancellationToken cancellationToken);

    /// <summary>Lista os beneficios de uma familia.</summary>
    /// <param name="familiaId">Identificador da familia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Beneficios da familia no tenant atual.</returns>
    Task<IReadOnlyList<Beneficio>> ListarPorFamiliaAsync(Guid familiaId, CancellationToken cancellationToken);

    /// <summary>Lista os beneficios concedidos numa competencia (apenas <see cref="SituacaoBeneficio.Concedida"/>).</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Concessoes da competencia no tenant atual.</returns>
    Task<IReadOnlyList<Beneficio>> ListarConcedidosPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken);
}
