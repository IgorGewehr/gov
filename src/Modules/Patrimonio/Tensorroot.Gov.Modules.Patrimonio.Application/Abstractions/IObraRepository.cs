using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Obra"/>.</summary>
public interface IObraRepository
{
    /// <summary>Marca uma nova obra para inserção.</summary>
    /// <param name="obra">Obra a adicionar.</param>
    void Adicionar(Obra obra);

    /// <summary>Obtém uma obra por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A obra, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Obra?> ObterPorIdAsync(ObraId id, CancellationToken cancellationToken);

    /// <summary>Indica se já existe uma obra vinculada ao contrato informado no tenant.</summary>
    /// <param name="contratoId">Contrato NLLC de origem.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o contrato já tiver obra aberta.</returns>
    Task<bool> ExisteParaContratoAsync(Guid contratoId, CancellationToken cancellationToken);

    /// <summary>Obtém a obra vinculada a um contrato (para aplicar aditivo de valor — I-2).</summary>
    /// <param name="contratoId">Contrato NLLC de origem.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A obra do contrato, ou <c>null</c> se não houver no tenant.</returns>
    Task<Obra?> ObterPorContratoAsync(Guid contratoId, CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de obras por objeto/município (navegabilidade — Onda 0), com filtro opcional por
    /// situação. Tenant-scoped via Global Query Filter. Ordena por objeto.
    /// </summary>
    /// <param name="termo">Termo livre (objeto ou município; case/acento-insensível); nulo lista tudo.</param>
    /// <param name="situacao">Filtro opcional por situação da obra.</param>
    /// <param name="pagina">Página (base 1).</param>
    /// <param name="tamanho">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Página de obras e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Obra> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoObra? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);

    /// <summary>Lista as obras em execução do tenant (base do varredor de prazos do art. 94 §3 — I-14).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Obras em execução.</returns>
    Task<IReadOnlyList<Obra>> ListarEmExecucaoAsync(CancellationToken cancellationToken);
}
