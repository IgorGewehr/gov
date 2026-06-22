using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Documento"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IDocumentoRepository
{
    /// <summary>Marca um novo documento para insercao.</summary>
    /// <param name="documento">Documento a adicionar.</param>
    void Adicionar(Documento documento);

    /// <summary>Obtem um documento por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O documento, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Documento?> ObterPorIdAsync(DocumentoId id, CancellationToken cancellationToken);

    /// <summary>Lista os documentos juntados a um processo (tenant-scoped; respeita o nivel de acesso).</summary>
    /// <param name="processoId">Processo de origem.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Documentos do processo no tenant.</returns>
    Task<IReadOnlyList<Documento>> ListarPorProcessoAsync(Guid processoId, CancellationToken cancellationToken);
}
