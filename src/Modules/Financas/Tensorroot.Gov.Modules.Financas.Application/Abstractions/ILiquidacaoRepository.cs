using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Liquidacao"/>.</summary>
public interface ILiquidacaoRepository
{
    /// <summary>Marca uma nova liquidação para inserção.</summary>
    /// <param name="liquidacao">Liquidação a adicionar.</param>
    void Adicionar(Liquidacao liquidacao);

    /// <summary>Obtém uma liquidação por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A liquidação, ou <c>null</c>.</returns>
    Task<Liquidacao?> ObterPorIdAsync(LiquidacaoId id, CancellationToken cancellationToken);

    /// <summary>Lista as liquidações de um empenho.</summary>
    /// <param name="empenhoId">Empenho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Liquidações do empenho.</returns>
    Task<IReadOnlyList<Liquidacao>> ListarPorEmpenhoAsync(EmpenhoId empenhoId, CancellationToken cancellationToken);
}
