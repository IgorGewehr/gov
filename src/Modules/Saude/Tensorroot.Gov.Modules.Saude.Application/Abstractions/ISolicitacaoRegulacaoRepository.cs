using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="SolicitacaoRegulacao"/>.</summary>
public interface ISolicitacaoRegulacaoRepository
{
    /// <summary>Marca uma nova solicitacao de regulacao para insercao.</summary>
    /// <param name="solicitacao">Solicitacao a adicionar.</param>
    void Adicionar(SolicitacaoRegulacao solicitacao);

    /// <summary>Obtem uma solicitacao por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A solicitacao, ou <c>null</c> se inexistente no tenant.</returns>
    Task<SolicitacaoRegulacao?> ObterPorIdAsync(SolicitacaoRegulacaoId id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista as solicitacoes pendentes (em analise: {<c>Solicitada</c>,<c>Devolvida</c>}) da fila de
    /// regulacao, filtrando opcionalmente por procedimento e prioridade, ordenadas por prioridade
    /// (desc) e data de solicitacao (asc). Sempre tenant-scoped.
    /// </summary>
    /// <param name="codigoSigtap">Filtro opcional por codigo SIGTAP.</param>
    /// <param name="prioridade">Filtro opcional por prioridade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Solicitacoes pendentes ordenadas para a fila de regulacao.</returns>
    Task<IReadOnlyList<SolicitacaoRegulacao>> ListarPendentesAsync(
        string? codigoSigtap,
        Prioridade? prioridade,
        CancellationToken cancellationToken);
}
