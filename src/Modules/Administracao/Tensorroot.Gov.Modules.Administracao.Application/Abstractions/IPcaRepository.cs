using Tensorroot.Gov.Modules.Administracao.Domain.Pca;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="PlanoContratacoes"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IPcaRepository
{
    /// <summary>Marca um novo plano para insercao.</summary>
    /// <param name="plano">Plano a adicionar.</param>
    void Adicionar(PlanoContratacoes plano);

    /// <summary>Obtem um plano por identificador (com itens carregados).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O plano, ou <c>null</c> se inexistente no tenant.</returns>
    Task<PlanoContratacoes?> ObterPorIdAsync(PlanoContratacoesId id, CancellationToken cancellationToken);

    /// <summary>Obtem o plano do exercicio informado, se existir.</summary>
    /// <param name="exercicio">Ano do exercicio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O plano do exercicio, ou <c>null</c>.</returns>
    Task<PlanoContratacoes?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe plano para o exercicio no tenant (unicidade).</summary>
    /// <param name="exercicio">Ano do exercicio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja houver plano para o exercicio.</returns>
    Task<bool> ExistePorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}
