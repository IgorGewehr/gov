using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="DispensaEletronica"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IDispensaRepository
{
    /// <summary>Marca uma nova dispensa para insercao.</summary>
    /// <param name="dispensa">Dispensa a adicionar.</param>
    void Adicionar(DispensaEletronica dispensa);

    /// <summary>Obtem uma dispensa por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A dispensa, ou <c>null</c> se inexistente no tenant.</returns>
    Task<DispensaEletronica?> ObterPorIdAsync(DispensaEletronicaId id, CancellationToken cancellationToken);

    /// <summary>Lista as dispensas do tenant na situacao informada.</summary>
    /// <param name="situacao">Situacao a filtrar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dispensas do tenant na situacao informada.</returns>
    Task<IReadOnlyList<DispensaEletronica>> ListarPorSituacaoAsync(SituacaoDispensa situacao, CancellationToken cancellationToken);
}
