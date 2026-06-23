using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do parque de equipamentos REP cadastrados (<see cref="RepConfigurado"/>), por tenant.</summary>
public interface IRepRepository
{
    /// <summary>Marca um novo REP para insercao.</summary>
    /// <param name="rep">Equipamento a cadastrar.</param>
    void Adicionar(RepConfigurado rep);

    /// <summary>Obtem um REP por identificador (no tenant atual).</summary>
    /// <param name="id">Identificador do REP.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O REP, ou <c>null</c> se nao existir no tenant.</returns>
    Task<RepConfigurado?> ObterPorIdAsync(RepConfiguradoId id, CancellationToken cancellationToken);

    /// <summary>Lista os REPs ATIVOS do tenant (alvos da coleta automatica do worker).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>REPs ativos do tenant.</returns>
    Task<IReadOnlyList<RepConfigurado>> ListarAtivosAsync(CancellationToken cancellationToken);
}
