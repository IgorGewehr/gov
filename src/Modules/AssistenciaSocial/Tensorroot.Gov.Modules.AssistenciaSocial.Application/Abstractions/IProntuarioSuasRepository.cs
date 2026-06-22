using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="ProntuarioSuas"/> — sempre tenant-scoped via Global Query
/// Filter; o conteudo sigiloso nunca cruza tenants (I-9). A trilha de acesso (<c>AcessoProntuario</c>)
/// e append-only (I-8).
/// </summary>
public interface IProntuarioSuasRepository
{
    /// <summary>Marca um novo prontuario para insercao.</summary>
    /// <param name="prontuario">Prontuario a adicionar.</param>
    void Adicionar(ProntuarioSuas prontuario);

    /// <summary>Obtem um prontuario por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O prontuario, ou <c>null</c> se inexistente no tenant.</returns>
    Task<ProntuarioSuas?> ObterPorIdAsync(ProntuarioSuasId id, CancellationToken cancellationToken);

    /// <summary>Obtem o prontuario de uma familia (tenant-scoped).</summary>
    /// <param name="familiaId">Familia acompanhada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O prontuario da familia, ou <c>null</c> se inexistente no tenant.</returns>
    Task<ProntuarioSuas?> ObterPorFamiliaAsync(Guid familiaId, CancellationToken cancellationToken);
}

/// <summary>
/// Consulta de leitura do <b>tipo</b> da unidade de atendimento (CRAS/CREAS/CentroPOP) do tenant,
/// usada para validar a compatibilidade servico↔unidade no registro de atendimento (PAIF↔CRAS,
/// PAEFI↔CREAS — I-4). Tenant-scoped. Definida em separado do agregado <c>UnidadeAtendimento</c>
/// (a ser gerado em iteracao futura deste mesmo modulo).
/// </summary>
public interface IUnidadeAtendimentoTipoLookup
{
    /// <summary>Obtem o tipo de uma unidade de atendimento por identificador (tenant-scoped).</summary>
    /// <param name="unidadeAtendimentoId">Identificador da unidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O tipo da unidade, ou <c>null</c> se inexistente no tenant.</returns>
    Task<TipoUnidadeAtendimento?> ObterTipoAsync(Guid unidadeAtendimentoId, CancellationToken cancellationToken);
}
