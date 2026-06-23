using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="FilaEspera"/> (sempre tenant-scoped via Global Query Filter).
/// </summary>
public interface IFilaEsperaRepository
{
    /// <summary>Marca uma nova entrada de fila para insercao.</summary>
    /// <param name="entrada">Entrada a adicionar.</param>
    void Adicionar(FilaEspera entrada);

    /// <summary>Obtem uma entrada da fila por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entrada, ou <c>null</c> se inexistente no tenant.</returns>
    Task<FilaEspera?> ObterPorIdAsync(FilaEsperaId id, CancellationToken cancellationToken);

    /// <summary>
    /// Obtem o PROXIMO da fila a convocar para um profissional/estabelecimento/tipo: somente entradas
    /// <see cref="SituacaoFilaEspera.Aguardando"/>, ordenadas por prioridade (desc) e data de entrada
    /// (FIFO dentro da prioridade). Tenant-scoped.
    /// </summary>
    /// <param name="profissionalId">Profissional da vaga liberada.</param>
    /// <param name="estabelecimentoId">Estabelecimento da vaga liberada.</param>
    /// <param name="tipo">Natureza da vaga liberada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A proxima entrada a convocar, ou <c>null</c> se a fila estiver vazia.</returns>
    Task<FilaEspera?> ObterProximoAConvocarAsync(
        ProfissionalId profissionalId,
        EstabelecimentoId estabelecimentoId,
        TipoAtendimentoAgenda tipo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista paginada da fila de espera por estabelecimento/situacao, ordenada por prioridade e data de
    /// entrada (ordem de convocacao). Tenant-scoped.
    /// </summary>
    /// <param name="estabelecimentoId">Filtro opcional por estabelecimento.</param>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de entradas e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<FilaEspera> Itens, int Total)> BuscarAsync(
        EstabelecimentoId? estabelecimentoId,
        SituacaoFilaEspera? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
