using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>Criterios de busca filtrada/paginada de normas juridicas (tenant-scoped).</summary>
/// <param name="Termo">Termo livre na ementa (opcional).</param>
/// <param name="Tipo">Tipo de norma (opcional).</param>
/// <param name="Numero">Numero exato (opcional).</param>
/// <param name="Ano">Ano exato (opcional).</param>
/// <param name="Situacao">Situacao de vigencia (opcional).</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record FiltroNormas(
    string? Termo,
    TipoNorma? Tipo,
    int? Numero,
    int? Ano,
    SituacaoVigencia? Situacao,
    int Pagina,
    int Tamanho);

/// <summary>Repositorio do agregado <see cref="Norma"/>.</summary>
public interface INormaRepository
{
    /// <summary>Marca uma nova norma para insercao.</summary>
    /// <param name="norma">Norma a adicionar.</param>
    void Adicionar(Norma norma);

    /// <summary>Obtem uma norma por identificador (com o historico de vigencia), respeitando o tenant.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A norma, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Norma?> ObterPorIdAsync(NormaId id, CancellationToken cancellationToken);

    /// <summary>Verifica a unicidade logica (tipo, numero, ano) no tenant — N-2.</summary>
    /// <param name="tipo">Tipo de norma.</param>
    /// <param name="numero">Numero.</param>
    /// <param name="ano">Ano.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existe norma com a mesma chave logica.</returns>
    Task<bool> ExisteAsync(TipoNorma tipo, int numero, int ano, CancellationToken cancellationToken);

    /// <summary>Busca filtrada/paginada de normas do tenant.</summary>
    /// <param name="filtro">Criterios de busca.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de normas e o total de itens.</returns>
    Task<(IReadOnlyList<Norma> Itens, int Total)> BuscarAsync(FiltroNormas filtro, CancellationToken cancellationToken);
}
