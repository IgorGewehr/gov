namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;

/// <summary>
/// Projecao minima de uma Unidade de Atendimento (CRAS/CREAS/CentroPOP) necessaria ao
/// referenciamento: identidade e territorio de cobertura. O agregado completo
/// <c>UnidadeAtendimento</c> sera gerado em iteracao futura deste mesmo modulo.
/// </summary>
/// <param name="Id">Identificador da unidade de atendimento.</param>
/// <param name="TerritorioCobertura">Territorio coberto pela unidade.</param>
public sealed record UnidadeAtendimentoResumo(Guid Id, string TerritorioCobertura);

/// <summary>
/// Repositorio de leitura das Unidades de Atendimento (CRAS) do tenant, usado pelo
/// referenciamento para validar a cobertura territorial (I-2). Tenant-scoped.
/// </summary>
public interface IUnidadeAtendimentoRepository
{
    /// <summary>Obtem o resumo de uma unidade de atendimento por identificador (tenant-scoped).</summary>
    /// <param name="unidadeAtendimentoId">Identificador da unidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O resumo da unidade, ou <c>null</c> se inexistente no tenant.</returns>
    Task<UnidadeAtendimentoResumo?> ObterPorIdAsync(Guid unidadeAtendimentoId, CancellationToken cancellationToken);
}
