using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Censo;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Censo;

/// <summary>Repositorio do agregado <see cref="UnidadeSocioassistencial"/> (tenant-scoped).</summary>
public interface IUnidadeSocioassistencialRepository
{
    /// <summary>Adiciona uma nova unidade.</summary>
    /// <param name="unidade">Unidade a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(UnidadeSocioassistencial unidade, CancellationToken cancellationToken);

    /// <summary>Obtem a unidade por identificador (com seus servicos), tenant-scoped.</summary>
    /// <param name="id">Identificador da unidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A unidade, ou <c>null</c> se inexistente no tenant.</returns>
    Task<UnidadeSocioassistencial?> ObterPorIdAsync(UnidadeSocioassistencialId id, CancellationToken cancellationToken);

    /// <summary>Lista as unidades socioassistenciais do tenant.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Unidades do tenant atual.</returns>
    Task<IReadOnlyList<UnidadeSocioassistencial>> ListarAsync(CancellationToken cancellationToken);
}

/// <summary>Repositorio do agregado <see cref="FormularioCensoSuas"/> (tenant-scoped).</summary>
public interface IFormularioCensoSuasRepository
{
    /// <summary>Adiciona um novo formulario.</summary>
    /// <param name="formulario">Formulario a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(FormularioCensoSuas formulario, CancellationToken cancellationToken);

    /// <summary>Obtem o formulario de uma unidade num exercicio (chave de negocio), tenant-scoped.</summary>
    /// <param name="unidadeId">Unidade consolidada.</param>
    /// <param name="exercicio">Exercicio (ano).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O formulario, ou <c>null</c> se ainda nao aberto no tenant.</returns>
    Task<FormularioCensoSuas?> ObterPorUnidadeExercicioAsync(UnidadeSocioassistencialId unidadeId, int exercicio, CancellationToken cancellationToken);

    /// <summary>Lista os formularios de um exercicio, tenant-scoped.</summary>
    /// <param name="exercicio">Exercicio (ano).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Formularios do exercicio no tenant atual.</returns>
    Task<IReadOnlyList<FormularioCensoSuas>> ListarPorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>
/// 3d.2 — fonte de consolidacao do Censo SUAS a partir das entidades JA existentes (RMA + Familia),
/// tenant-scoped. DERIVA o volume anual de atendimentos (somatorio dos RMAs da unidade no exercicio) e a
/// quantidade de familias referenciadas a unidade — sem dupla digitacao e sem trafegar dado sigiloso.
/// </summary>
public interface IConsolidacaoCensoReadModel
{
    /// <summary>
    /// Soma o total de atendimentos de todos os RMAs (qualquer competencia) de uma unidade no exercicio.
    /// </summary>
    /// <param name="unidadeAtendimentoId">Unidade (Id reusado entre Censo/RMA/Familia).</param>
    /// <param name="exercicio">Exercicio (ano).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Volume anual de atendimentos consolidado.</returns>
    Task<int> SomarAtendimentosDoExercicioAsync(Guid unidadeAtendimentoId, int exercicio, CancellationToken cancellationToken);

    /// <summary>Conta as familias referenciadas a uma unidade (do cadastro de familias), tenant-scoped.</summary>
    /// <param name="unidadeAtendimentoId">Unidade (Id reusado entre Censo/RMA/Familia).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de familias referenciadas a unidade.</returns>
    Task<int> ContarFamiliasReferenciadasAsync(Guid unidadeAtendimentoId, CancellationToken cancellationToken);
}
