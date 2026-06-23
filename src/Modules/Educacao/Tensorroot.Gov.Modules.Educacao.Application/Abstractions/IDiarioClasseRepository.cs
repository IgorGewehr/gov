using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Abstractions;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;

/// <summary>Repositorio do agregado <see cref="DiarioClasseAggregate"/>.</summary>
public interface IDiarioClasseRepository
{
    /// <summary>Marca um novo diario para insercao.</summary>
    /// <param name="diario">Diario a adicionar.</param>
    void Adicionar(DiarioClasseAggregate diario);

    /// <summary>Obtem um diario por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador do diario.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O diario, ou <c>null</c> se inexistente no tenant.</returns>
    Task<DiarioClasseAggregate?> ObterPorIdAsync(DiarioClasseId id, CancellationToken cancellationToken);

    /// <summary>Obtem o diario vinculado a uma matricula (vinculo 1-1 — I-7), respeitando o filtro de tenant.</summary>
    /// <param name="matriculaId">Matricula vinculada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O diario da matricula, ou <c>null</c> se inexistente no tenant.</returns>
    Task<DiarioClasseAggregate?> ObterPorMatriculaAsync(MatriculaId matriculaId, CancellationToken cancellationToken);

    /// <summary>
    /// Carrega os diarios (com colecoes filhas) vinculados a um conjunto de matriculas — base do
    /// lancamento em lote do diario coletivo da turma (sub-onda 3a). Respeita o filtro de tenant.
    /// </summary>
    /// <param name="matriculaIds">Matriculas cujas raizes de diario carregar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Diarios encontrados (pode ser menor que a entrada quando alguma matricula nao tem diario).</returns>
    Task<IReadOnlyList<DiarioClasseAggregate>> ListarPorMatriculasAsync(
        IReadOnlyCollection<MatriculaId> matriculaIds,
        CancellationToken cancellationToken);

    /// <summary>Indica se ja existe diario para a matricula informada (vinculo 1-1 — I-7).</summary>
    /// <param name="matriculaId">Matricula vinculada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja houver diario para a matricula no tenant.</returns>
    Task<bool> ExisteParaMatriculaAsync(MatriculaId matriculaId, CancellationToken cancellationToken);
}
