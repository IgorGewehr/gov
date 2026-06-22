using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="DotacaoOrcamentaria"/>.</summary>
public interface IDotacaoOrcamentariaRepository
{
    /// <summary>Marca uma nova dotação para inserção.</summary>
    /// <param name="dotacao">Dotação a adicionar.</param>
    void Adicionar(DotacaoOrcamentaria dotacao);

    /// <summary>Obtém uma dotação por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A dotação, ou <c>null</c>.</returns>
    Task<DotacaoOrcamentaria?> ObterPorIdAsync(DotacaoOrcamentariaId id, CancellationToken cancellationToken);

    /// <summary>Lista as dotações de um exercício.</summary>
    /// <param name="exercicio">Exercício orçamentário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dotações do exercício.</returns>
    Task<IReadOnlyList<DotacaoOrcamentaria>> ListarPorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}
