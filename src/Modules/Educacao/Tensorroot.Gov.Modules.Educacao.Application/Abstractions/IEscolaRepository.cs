using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Escola"/> (todas as operacoes sao tenant-scoped).</summary>
public interface IEscolaRepository
{
    /// <summary>Marca uma nova escola para insercao.</summary>
    /// <param name="escola">Escola a adicionar.</param>
    void Adicionar(Escola escola);

    /// <summary>Obtem uma escola por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A escola, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Escola?> ObterPorIdAsync(EscolaId id, CancellationToken cancellationToken);

    /// <summary>Obtem uma escola por codigo INEP (respeitando o filtro de tenant).</summary>
    /// <param name="codigoInep">Codigo INEP a buscar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A escola, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Escola?> ObterPorCodigoInepAsync(CodigoInep codigoInep, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe uma escola com o codigo INEP informado na rede (I-1).</summary>
    /// <param name="codigoInep">Codigo INEP a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o codigo INEP ja estiver cadastrado no tenant.</returns>
    Task<bool> ExisteCodigoInepAsync(CodigoInep codigoInep, CancellationToken cancellationToken);

    /// <summary>Lista todas as escolas da rede (tenant atual).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Escolas do tenant.</returns>
    Task<IReadOnlyList<Escola>> ListarAsync(CancellationToken cancellationToken);
}
