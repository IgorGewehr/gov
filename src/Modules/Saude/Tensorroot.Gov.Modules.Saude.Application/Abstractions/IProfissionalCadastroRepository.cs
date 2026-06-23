using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="Profissional"/> (equipe das unidades; sempre tenant-scoped via
/// Global Query Filter). Distinto da porta de leitura/ACL <see cref="IEstabelecimentoRepository"/>.
/// </summary>
public interface IProfissionalCadastroRepository
{
    /// <summary>Marca um novo profissional para insercao.</summary>
    /// <param name="profissional">Profissional a adicionar.</param>
    void Adicionar(Profissional profissional);

    /// <summary>Obtem um profissional por identificador (com seus vinculos; respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O profissional, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Profissional?> ObterPorIdAsync(ProfissionalId id, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe profissional com o CPF informado no tenant atual.</summary>
    /// <param name="cpf">CPF do profissional.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir; caso contrario, <c>false</c>.</returns>
    Task<bool> ExistePorCpfAsync(Cpf cpf, CancellationToken cancellationToken);

    /// <summary>
    /// Busca paginada de profissionais por nome/CPF e filtros por CBO/estabelecimento/situacao
    /// (navegabilidade). Tenant-scoped via Global Query Filter. Ordena por nome.
    /// </summary>
    /// <param name="termo">Termo livre (nome ou CPF); nulo lista tudo.</param>
    /// <param name="cbo">Filtro opcional por CBO (vinculo).</param>
    /// <param name="estabelecimentoId">Filtro opcional por estabelecimento (vinculo).</param>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de profissionais e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Profissional> Itens, int Total)> BuscarAsync(
        string? termo,
        string? cbo,
        Guid? estabelecimentoId,
        SituacaoProfissional? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
