using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="Estabelecimento"/> (master data CNES local; sempre tenant-scoped
/// via Global Query Filter). Distinto da porta de leitura/ACL <see cref="IEstabelecimentoRepository"/>,
/// que valida estabelecimento/profissional ativos na competencia do atendimento.
/// </summary>
public interface IEstabelecimentoCadastroRepository
{
    /// <summary>Marca um novo estabelecimento para insercao.</summary>
    /// <param name="estabelecimento">Estabelecimento a adicionar.</param>
    void Adicionar(Estabelecimento estabelecimento);

    /// <summary>Obtem um estabelecimento por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O estabelecimento, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Estabelecimento?> ObterPorIdAsync(EstabelecimentoId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe estabelecimento com o CNES informado no tenant atual.</summary>
    /// <param name="cnes">Codigo CNES.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir; caso contrario, <c>false</c>.</returns>
    Task<bool> ExistePorCnesAsync(CodigoCnes cnes, CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de estabelecimentos por nome/CNES e filtros por tipo/situacao (navegabilidade).
    /// Tenant-scoped via Global Query Filter. Ordena por nome.
    /// </summary>
    /// <param name="termo">Termo livre (nome ou CNES); nulo lista tudo.</param>
    /// <param name="tipo">Filtro opcional por tipo.</param>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de estabelecimentos e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Estabelecimento> Itens, int Total)> BuscarAsync(
        string? termo,
        TipoEstabelecimento? tipo,
        SituacaoEstabelecimento? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
