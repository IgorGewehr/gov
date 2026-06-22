using Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Comissao"/>.</summary>
public interface IComissaoRepository
{
    /// <summary>Marca uma nova comissao para insercao.</summary>
    /// <param name="comissao">Comissao a adicionar.</param>
    void Adicionar(Comissao comissao);

    /// <summary>Obtem uma comissao por identificador (com membros), respeitando o tenant.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A comissao, ou <c>null</c>.</returns>
    Task<Comissao?> ObterPorIdAsync(ComissaoId id, CancellationToken cancellationToken);

    /// <summary>Lista as comissoes do tenant (ordenadas por nome).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Comissoes do tenant.</returns>
    Task<IReadOnlyList<Comissao>> ListarAsync(CancellationToken cancellationToken);
}
