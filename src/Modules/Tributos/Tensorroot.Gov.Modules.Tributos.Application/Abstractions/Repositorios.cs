using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;

namespace Tensorroot.Gov.Modules.Tributos.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Contribuinte"/>.</summary>
public interface IContribuinteRepository
{
    /// <summary>Marca um novo contribuinte para inserção.</summary>
    /// <param name="contribuinte">Contribuinte a adicionar.</param>
    void Adicionar(Contribuinte contribuinte);

    /// <summary>Obtém um contribuinte por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O contribuinte, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Contribuinte?> ObterPorIdAsync(ContribuinteId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="Lancamento"/>.</summary>
public interface ILancamentoRepository
{
    /// <summary>Marca um novo lançamento para inserção.</summary>
    /// <param name="lancamento">Lançamento a adicionar.</param>
    void Adicionar(Lancamento lancamento);

    /// <summary>Obtém um lançamento por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O lançamento, ou <c>null</c>.</returns>
    Task<Lancamento?> ObterPorIdAsync(LancamentoId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="DividaAtiva"/>.</summary>
public interface IDividaAtivaRepository
{
    /// <summary>Marca uma nova dívida ativa para inserção.</summary>
    /// <param name="dividaAtiva">Dívida ativa a adicionar.</param>
    void Adicionar(DividaAtiva dividaAtiva);

    /// <summary>Obtém uma dívida ativa por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A dívida ativa, ou <c>null</c>.</returns>
    Task<DividaAtiva?> ObterPorIdAsync(DividaAtivaId id, CancellationToken cancellationToken);

    /// <summary>Lista as dívidas ativas de um contribuinte.</summary>
    /// <param name="contribuinteId">Contribuinte.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dívidas ativas do contribuinte.</returns>
    Task<IReadOnlyList<DividaAtiva>> ListarPorContribuinteAsync(ContribuinteId contribuinteId, CancellationToken cancellationToken);
}
