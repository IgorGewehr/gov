using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

namespace Tensorroot.Gov.Modules.Educacao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Cardapio"/> (cardapio semanal do PNAE). Tenant-scoped.</summary>
public interface ICardapioRepository
{
    /// <summary>Marca um novo cardapio para insercao.</summary>
    /// <param name="cardapio">Cardapio a adicionar.</param>
    void Adicionar(Cardapio cardapio);

    /// <summary>Obtem um cardapio por identificador, com os itens carregados (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O cardapio, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Cardapio?> ObterPorIdAsync(CardapioId id, CancellationToken cancellationToken);

    /// <summary>Lista cardapios por escola e/ou semana (picker do front). Tenant-scoped.</summary>
    /// <param name="escolaId">Filtro opcional por escola.</param>
    /// <param name="semana">Filtro opcional pela semana (segunda-feira).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Cardapios que atendem ao filtro.</returns>
    Task<IReadOnlyList<Cardapio>> ListarAsync(EscolaId? escolaId, DateOnly? semana, CancellationToken cancellationToken);
}

/// <summary>Repositorio do agregado <see cref="DistribuicaoMerenda"/> (consumo efetivo do dia). Tenant-scoped.</summary>
public interface IDistribuicaoMerendaRepository
{
    /// <summary>Marca uma nova distribuicao para insercao.</summary>
    /// <param name="distribuicao">Distribuicao a adicionar.</param>
    void Adicionar(DistribuicaoMerenda distribuicao);

    /// <summary>
    /// Lista distribuicoes por escola em um periodo (base do relatorio de consumo PNAE). Tenant-scoped;
    /// inclui os consumos por genero.
    /// </summary>
    /// <param name="escolaId">Escola.</param>
    /// <param name="de">Inicio do periodo (inclusivo).</param>
    /// <param name="ate">Fim do periodo (inclusivo).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Distribuicoes do periodo.</returns>
    Task<IReadOnlyList<DistribuicaoMerenda>> ListarPorEscolaEPeriodoAsync(
        EscolaId escolaId,
        DateOnly de,
        DateOnly ate,
        CancellationToken cancellationToken);
}
