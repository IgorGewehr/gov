using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Portaria"/> (atos de pessoal).</summary>
public interface IPortariaRepository
{
    /// <summary>Marca uma nova portaria para insercao.</summary>
    /// <param name="portaria">Portaria a adicionar.</param>
    void Adicionar(Portaria portaria);

    /// <summary>Obtem uma portaria por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A portaria, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Portaria?> ObterPorIdAsync(PortariaId id, CancellationToken cancellationToken);

    /// <summary>
    /// Apura o proximo sequencial da numeracao de portaria para o exercicio informado no tenant
    /// (max sequencial do exercicio + 1; inicia em <see cref="NumeroPortaria.SequencialMinimo"/>).
    /// </summary>
    /// <param name="exercicio">Exercicio (ano civil).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Proximo sequencial disponivel no exercicio.</returns>
    Task<int> ProximoSequencialAsync(int exercicio, CancellationToken cancellationToken);

    /// <summary>Busca paginada de portarias por tipo/situacao/exercicio/servidor (navegabilidade).</summary>
    /// <param name="tipo">Filtro opcional por natureza do ato.</param>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="exercicio">Filtro opcional por exercicio.</param>
    /// <param name="servidorId">Filtro opcional por servidor vinculado.</param>
    /// <param name="pagina">Pagina (1-based).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Itens da pagina e total filtrado.</returns>
    Task<(IReadOnlyList<Portaria> Itens, int Total)> BuscarAsync(
        TipoPortaria? tipo,
        SituacaoPortaria? situacao,
        int? exercicio,
        Guid? servidorId,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
