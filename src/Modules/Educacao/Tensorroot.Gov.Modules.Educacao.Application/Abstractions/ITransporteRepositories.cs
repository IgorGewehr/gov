using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

namespace Tensorroot.Gov.Modules.Educacao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="RotaTransporte"/> (rota PNATE). Tenant-scoped.</summary>
public interface IRotaTransporteRepository
{
    /// <summary>Marca uma nova rota para insercao.</summary>
    /// <param name="rota">Rota a adicionar.</param>
    void Adicionar(RotaTransporte rota);

    /// <summary>Obtem uma rota por identificador, com os alunos carregados (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A rota, ou <c>null</c> se inexistente no tenant.</returns>
    Task<RotaTransporte?> ObterPorIdAsync(RotaTransporteId id, CancellationToken cancellationToken);

    /// <summary>Lista rotas por escola (picker do front). Tenant-scoped.</summary>
    /// <param name="escolaId">Filtro opcional por escola.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Rotas que atendem ao filtro.</returns>
    Task<IReadOnlyList<RotaTransporte>> ListarPorEscolaAsync(EscolaId? escolaId, CancellationToken cancellationToken);
}
