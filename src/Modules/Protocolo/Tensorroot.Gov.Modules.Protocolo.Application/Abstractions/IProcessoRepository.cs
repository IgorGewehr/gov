using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="Processo"/> (modulo Protocolo). Expoe a porta de leitura de
/// existencia (consumida pela juntada de documentos) e as operacoes de escrita/consulta do ciclo de
/// vida do PAE. Toda consulta e tenant-scoped via Global Query Filter; a implementacao reside na
/// Infrastructure do Protocolo.
/// </summary>
public interface IProcessoRepository
{
    /// <summary>Indica se existe um processo com o identificador informado no tenant corrente.</summary>
    /// <param name="processoId">Identificador do processo de destino.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o processo existir no tenant.</returns>
    Task<bool> ExisteAsync(Guid processoId, CancellationToken cancellationToken);

    /// <summary>Marca um novo processo para insercao.</summary>
    /// <param name="processo">Processo a adicionar.</param>
    void Adicionar(Processo processo);

    /// <summary>Obtem um processo por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O processo, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Processo?> ObterPorIdAsync(ProcessoId id, CancellationToken cancellationToken);

    /// <summary>Obtem um processo pelo NUP (respeitando o filtro de tenant).</summary>
    /// <param name="nup">Numero Unico de Protocolo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O processo, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Processo?> ObterPorNupAsync(Nup nup, CancellationToken cancellationToken);

    /// <summary>Lista os processos cujo setor atual e o informado (tenant-scoped).</summary>
    /// <param name="setorId">Setor atual responsavel.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Processos do setor no tenant.</returns>
    Task<IReadOnlyList<Processo>> ListarPorSetorAtualAsync(Guid setorId, CancellationToken cancellationToken);
}
