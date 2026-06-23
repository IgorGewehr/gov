using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="LancamentoContabil"/>.</summary>
public interface ILancamentoContabilRepository
{
    /// <summary>Marca um novo lançamento para inserção.</summary>
    /// <param name="lancamento">Lançamento a adicionar.</param>
    void Adicionar(LancamentoContabil lancamento);

    /// <summary>Obtém um lançamento por identificador (com partidas).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O lançamento, ou <c>null</c>.</returns>
    Task<LancamentoContabil?> ObterPorIdAsync(LancamentoContabilId id, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se já existe lançamento gerado a partir do fato de origem informado (idempotência do
    /// lançamento automático em caso de retry).
    /// </summary>
    /// <param name="origemReferenciaId">Id do fato de origem.</param>
    /// <param name="eventoContabilId">Roteiro aplicado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já existir.</returns>
    Task<bool> ExisteParaOrigemAsync(Guid origemReferenciaId, Guid eventoContabilId, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se já existe lançamento para a origem informada, independentemente de roteiro
    /// (usado pelo encerramento de exercício, cujos lançamentos não têm <c>EventoContabilId</c>).
    /// </summary>
    /// <param name="origemReferenciaId">Id determinístico da fase/conta de encerramento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já existir.</returns>
    Task<bool> ExisteParaOrigemAsync(Guid origemReferenciaId, CancellationToken cancellationToken);
}
