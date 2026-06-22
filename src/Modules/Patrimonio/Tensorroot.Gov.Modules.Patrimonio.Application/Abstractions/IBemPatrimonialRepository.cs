using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="BemPatrimonial"/>.</summary>
public interface IBemPatrimonialRepository
{
    /// <summary>Marca um novo bem patrimonial para inserção.</summary>
    /// <param name="bemPatrimonial">Bem a adicionar.</param>
    void Adicionar(BemPatrimonial bemPatrimonial);

    /// <summary>Obtém um bem por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O bem, ou <c>null</c> se inexistente no tenant.</returns>
    Task<BemPatrimonial?> ObterPorIdAsync(BemPatrimonialId id, CancellationToken cancellationToken);

    /// <summary>Lista os bens depreciáveis do tenant (Tombado, em condições de uso, valor contábil acima do residual).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Bens depreciáveis do tenant.</returns>
    Task<IReadOnlyList<BemPatrimonial>> ListarDepreciaveisAsync(CancellationToken cancellationToken);
}
