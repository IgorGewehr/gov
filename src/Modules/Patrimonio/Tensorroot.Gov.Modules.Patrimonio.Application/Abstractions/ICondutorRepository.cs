using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Condutor"/>.</summary>
public interface ICondutorRepository
{
    /// <summary>Marca um novo condutor para inserção.</summary>
    /// <param name="condutor">Condutor a adicionar.</param>
    void Adicionar(Condutor condutor);

    /// <summary>Obtém um condutor por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O condutor, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Condutor?> ObterPorIdAsync(CondutorId id, CancellationToken cancellationToken);

    /// <summary>Indica se já existe um condutor com o número de CNH informado no tenant.</summary>
    /// <param name="numeroCnh">Número da CNH a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a CNH já estiver cadastrada.</returns>
    Task<bool> ExisteCnhAsync(string numeroCnh, CancellationToken cancellationToken);

    /// <summary>Busca paginada de condutores por nome/CPF/CNH, filtro opcional por situação.</summary>
    /// <param name="termo">Termo livre; nulo lista tudo.</param>
    /// <param name="situacao">Filtro opcional por situação do condutor.</param>
    /// <param name="pagina">Página (base 1).</param>
    /// <param name="tamanho">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Página de condutores e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Condutor> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoCondutor? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista os condutores com CNH a vencer dentro da janela informada a partir da data de referência
    /// (inclui CNH já vencida, com dias negativos) — base do alerta e do bloqueio de viagem. Tenant-scoped.
    /// </summary>
    /// <param name="referencia">Data de referência (hoje).</param>
    /// <param name="ate">Limite superior da janela de vencimento (inclusivo).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Condutores com CNH a vencer/vencida na janela.</returns>
    Task<IReadOnlyList<Condutor>> ListarCnhVencendoAsync(
        DateOnly referencia,
        DateOnly ate,
        CancellationToken cancellationToken);
}
