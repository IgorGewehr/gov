using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do cadastro mestre de <see cref="Consignataria"/> (tenant-scoped via Global Query Filter).</summary>
public interface IConsignatariaRepository
{
    /// <summary>Marca uma nova consignataria para insercao.</summary>
    /// <param name="consignataria">Consignataria a adicionar.</param>
    void Adicionar(Consignataria consignataria);

    /// <summary>Obtem uma consignataria por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A consignataria, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Consignataria?> ObterPorIdAsync(ConsignatariaId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe consignataria com o CNPJ no tenant (unicidade (TenantId, Cnpj)).</summary>
    /// <param name="cnpj">CNPJ a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir.</returns>
    Task<bool> ExistePorCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken);

    /// <summary>Lista as consignatarias do tenant (cadastro mestre), ordenadas por razao social.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Consignatarias do tenant.</returns>
    Task<IReadOnlyList<Consignataria>> ListarAsync(CancellationToken cancellationToken);
}
