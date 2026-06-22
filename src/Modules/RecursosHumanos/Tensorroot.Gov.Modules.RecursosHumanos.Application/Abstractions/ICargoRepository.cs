using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Cargo"/>.</summary>
public interface ICargoRepository
{
    /// <summary>Marca um novo cargo para insercao.</summary>
    /// <param name="cargo">Cargo a adicionar.</param>
    void Adicionar(Cargo cargo);

    /// <summary>Obtem um cargo por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O cargo, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Cargo?> ObterPorIdAsync(CargoId id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista os cargos com vagas disponiveis (situacao diferente de <see cref="SituacaoCargo.Extinto"/>
    /// e <c>VagasOcupadas &lt; QuantidadeVagas</c>), opcionalmente filtrados por tipo.
    /// </summary>
    /// <param name="tipo">Filtro opcional por tipo de cargo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Cargos com vagas disponiveis no tenant.</returns>
    Task<IReadOnlyList<Cargo>> ListarComVagasDisponiveisAsync(TipoCargo? tipo, CancellationToken cancellationToken);
}
