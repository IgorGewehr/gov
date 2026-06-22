using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Paciente"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IPacienteRepository
{
    /// <summary>Marca um novo paciente para insercao.</summary>
    /// <param name="paciente">Paciente a adicionar.</param>
    void Adicionar(Paciente paciente);

    /// <summary>Obtem um paciente por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O paciente, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Paciente?> ObterPorIdAsync(PacienteId id, CancellationToken cancellationToken);

    /// <summary>Obtem um paciente pelo CNS (respeitando o filtro de tenant).</summary>
    /// <param name="cns">Cartao Nacional de Saude.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O paciente, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Paciente?> ObterPorCnsAsync(Cns cns, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe um paciente com o CNS informado no tenant atual (I-9).</summary>
    /// <param name="cns">Cartao Nacional de Saude.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir; caso contrario, <c>false</c>.</returns>
    Task<bool> ExistePorCnsAsync(Cns cns, CancellationToken cancellationToken);
}
