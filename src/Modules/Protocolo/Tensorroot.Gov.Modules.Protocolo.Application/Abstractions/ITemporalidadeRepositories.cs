using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="PlanoDeClassificacao"/> (Peca 2 / W9.4). Tenant-scoped via
/// Global Query Filter; a implementacao reside na Infrastructure do Protocolo.
/// </summary>
public interface IPlanoDeClassificacaoRepository
{
    /// <summary>Marca um novo plano para insercao.</summary>
    /// <param name="plano">Plano a adicionar.</param>
    void Adicionar(PlanoDeClassificacao plano);

    /// <summary>Obtem o plano de classificacao ATIVO do tenant corrente (ou nulo).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O plano ativo, ou <c>null</c>.</returns>
    Task<PlanoDeClassificacao?> ObterAtivoAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Repositorio do agregado <see cref="TabelaTemporalidade"/> (Peca 2 / W9.4). Tenant-scoped.
/// </summary>
public interface ITabelaTemporalidadeRepository
{
    /// <summary>Marca uma nova TTD para insercao.</summary>
    /// <param name="tabela">TTD a adicionar.</param>
    void Adicionar(TabelaTemporalidade tabela);

    /// <summary>Obtem a TTD ATIVA do tenant corrente (ou nulo).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A TTD ativa, ou <c>null</c>.</returns>
    Task<TabelaTemporalidade?> ObterAtivaAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Repositorio do agregado <see cref="DestinacaoProcesso"/> (Peca 2 / W9.4). Tenant-scoped.
/// </summary>
public interface IDestinacaoProcessoRepository
{
    /// <summary>Marca uma nova ficha de destinacao para insercao.</summary>
    /// <param name="destinacao">Ficha a adicionar.</param>
    void Adicionar(DestinacaoProcesso destinacao);

    /// <summary>Obtem a ficha de destinacao de um processo (ou nulo).</summary>
    /// <param name="processoId">Processo arquivado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ficha, ou <c>null</c>.</returns>
    Task<DestinacaoProcesso?> ObterPorProcessoAsync(Guid processoId, CancellationToken cancellationToken);

    /// <summary>Obtem a ficha de destinacao por identificador (ou nulo).</summary>
    /// <param name="id">Identificador da ficha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A ficha, ou <c>null</c>.</returns>
    Task<DestinacaoProcesso?> ObterPorIdAsync(DestinacaoProcessoId id, CancellationToken cancellationToken);

    /// <summary>Lista fichas em <see cref="EstadoDestinacao.AguardandoPrazo"/> (varredura de aptidao).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Fichas aguardando prazo no tenant.</returns>
    Task<IReadOnlyList<DestinacaoProcesso>> ListarAguardandoPrazoAsync(CancellationToken cancellationToken);
}
