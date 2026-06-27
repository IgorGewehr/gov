using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Pneu"/>.</summary>
public interface IPneuRepository
{
    /// <summary>Marca um novo pneu para inserção.</summary>
    /// <param name="pneu">Pneu a adicionar.</param>
    void Adicionar(Pneu pneu);

    /// <summary>Obtém um pneu por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O pneu, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Pneu?> ObterPorIdAsync(PneuId id, CancellationToken cancellationToken);

    /// <summary>Indica se já existe um pneu com o número de fogo informado no tenant.</summary>
    /// <param name="numeroFogo">Número de fogo a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o número de fogo já estiver em uso.</returns>
    Task<bool> ExisteNumeroFogoAsync(string numeroFogo, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se já existe outro pneu (diferente do informado) instalado na mesma posição do mesmo
    /// veículo — guarda a unicidade de slot do layout de eixos (uma posição, um pneu por vez).
    /// </summary>
    /// <param name="veiculoId">Veículo.</param>
    /// <param name="posicao">Posição (eixo/lado).</param>
    /// <param name="exceto">Pneu a desconsiderar (o que está sendo instalado).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a posição já estiver ocupada por outro pneu.</returns>
    Task<bool> PosicaoOcupadaAsync(VeiculoId veiculoId, PosicaoPneu posicao, PneuId exceto, CancellationToken cancellationToken);

    /// <summary>Busca paginada de pneus por número de fogo/marca/modelo/medida, filtro opcional por situação.</summary>
    /// <param name="termo">Termo livre; nulo lista tudo.</param>
    /// <param name="situacao">Filtro opcional por situação do pneu.</param>
    /// <param name="pagina">Página (base 1).</param>
    /// <param name="tamanho">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Página de pneus e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Pneu> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoPneu? situacao,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);

    /// <summary>Lista os pneus atualmente instalados em um veículo, com a posição de montagem (layout de eixos).</summary>
    /// <param name="veiculoId">Veículo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pneus instalados no veículo.</returns>
    Task<IReadOnlyList<Pneu>> ListarInstaladosDoVeiculoAsync(VeiculoId veiculoId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista os pneus em rodagem cujo sulco atual atingiu o mínimo legal informado ou ficou abaixo dele
    /// (CONTRAN/CTB) — candidatos a remoção/recapagem/descarte. Tenant-scoped.
    /// </summary>
    /// <param name="sulcoMinimoMilimetros">Sulco mínimo legal vigente (mm), parametrizável por tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pneus instalados no limite ou abaixo do sulco mínimo.</returns>
    Task<IReadOnlyList<Pneu>> ListarNoLimiteDeSulcoAsync(decimal sulcoMinimoMilimetros, CancellationToken cancellationToken);
}
