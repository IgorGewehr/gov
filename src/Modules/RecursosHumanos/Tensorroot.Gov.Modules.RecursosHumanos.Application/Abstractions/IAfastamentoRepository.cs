using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Afastamento"/> (tenant-scoped via Global Query Filter).</summary>
public interface IAfastamentoRepository
{
    /// <summary>Marca um novo afastamento para insercao.</summary>
    /// <param name="afastamento">Afastamento a adicionar.</param>
    void Adicionar(Afastamento afastamento);

    /// <summary>Obtem um afastamento por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O afastamento, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Afastamento?> ObterPorIdAsync(AfastamentoId id, CancellationToken cancellationToken);

    /// <summary>Obtem o afastamento VIGENTE do servidor (no maximo um), se houver.</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O afastamento vigente, ou <c>null</c>.</returns>
    Task<Afastamento?> ObterVigenteDoServidorAsync(ServidorId servidorId, CancellationToken cancellationToken);

    /// <summary>Lista os afastamentos do servidor que incidem (vigentes ou nao) sobre a competencia.</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="competencia">Competencia da folha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Afastamentos que sobrepoem a competencia.</returns>
    Task<IReadOnlyList<Afastamento>> ListarVigentesNaCompetenciaAsync(
        Guid servidorId,
        Competencia competencia,
        CancellationToken cancellationToken);

    /// <summary>Lista todos os afastamentos de um servidor (historico, mais recentes primeiro).</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Afastamentos do servidor.</returns>
    Task<IReadOnlyList<Afastamento>> ListarPorServidorAsync(Guid servidorId, CancellationToken cancellationToken);

    /// <summary>Busca paginada de afastamentos por tipo/situacao/competencia.</summary>
    /// <param name="tipo">Filtro opcional por tipo.</param>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="competencia">Filtro opcional por competencia (afastamentos que a sobrepoem).</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de afastamentos e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Afastamento> Itens, int Total)> BuscarAsync(
        TipoAfastamento? tipo,
        SituacaoAfastamento? situacao,
        Competencia? competencia,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
