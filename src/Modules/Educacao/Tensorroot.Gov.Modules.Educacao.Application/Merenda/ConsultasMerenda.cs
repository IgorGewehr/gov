using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

namespace Tensorroot.Gov.Modules.Educacao.Application.Merenda;

/// <summary>Obtem a ficha de um cardapio (com itens). Tenant-scoped; read-only.</summary>
/// <param name="CardapioId">Identificador do cardapio.</param>
public sealed record ObterCardapioQuery(Guid CardapioId) : IQuery<CardapioDto?>;

/// <summary>Handler de obtencao de cardapio.</summary>
public sealed class ObterCardapioHandler(ICardapioRepository cardapios)
    : IQueryHandler<ObterCardapioQuery, CardapioDto?>
{
    /// <inheritdoc />
    public async Task<CardapioDto?> Handle(ObterCardapioQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var cardapio = await cardapios.ObterPorIdAsync(new CardapioId(request.CardapioId), cancellationToken).ConfigureAwait(false);
        return cardapio is null ? null : MerendaMapeamento.ParaDto(cardapio);
    }
}

/// <summary>Lista cardapios por escola e/ou semana. Tenant-scoped; read-only.</summary>
/// <param name="EscolaId">Filtro opcional por escola.</param>
/// <param name="Semana">Filtro opcional pela semana (segunda-feira).</param>
public sealed record ListarCardapiosQuery(Guid? EscolaId, DateOnly? Semana) : IQuery<IReadOnlyList<CardapioDto>>;

/// <summary>Handler da listagem de cardapios.</summary>
public sealed class ListarCardapiosHandler(ICardapioRepository cardapios)
    : IQueryHandler<ListarCardapiosQuery, IReadOnlyList<CardapioDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CardapioDto>> Handle(ListarCardapiosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var escolaId = request.EscolaId is { } id ? new EscolaId(id) : (EscolaId?)null;
        var itens = await cardapios.ListarAsync(escolaId, request.Semana, cancellationToken).ConfigureAwait(false);
        return itens.Select(MerendaMapeamento.ParaDto).ToList();
    }
}

/// <summary>
/// Relatorio de consumo de generos por escola/periodo (agrega as distribuicoes). Base de gestao do
/// PNAE local; prestacao de contas ao FNDE = M10. Tenant-scoped; read-only.
/// </summary>
/// <param name="EscolaId">Escola.</param>
/// <param name="De">Inicio do periodo (inclusivo).</param>
/// <param name="Ate">Fim do periodo (inclusivo).</param>
public sealed record ObterConsumoMerendaQuery(Guid EscolaId, DateOnly De, DateOnly Ate)
    : IQuery<RelatorioConsumoDto>;

/// <summary>Handler do relatorio de consumo de merenda.</summary>
public sealed class ObterConsumoMerendaHandler(IDistribuicaoMerendaRepository distribuicoes)
    : IQueryHandler<ObterConsumoMerendaQuery, RelatorioConsumoDto>
{
    /// <inheritdoc />
    public async Task<RelatorioConsumoDto> Handle(ObterConsumoMerendaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var escolaId = new EscolaId(request.EscolaId);
        var itens = await distribuicoes
            .ListarPorEscolaEPeriodoAsync(escolaId, request.De, request.Ate, cancellationToken)
            .ConfigureAwait(false);

        var totalComensais = itens.Sum(distribuicao => distribuicao.Comensais);

        var generos = itens
            .SelectMany(distribuicao => distribuicao.Consumos)
            .GroupBy(consumo => new { consumo.GeneroEstoqueId, consumo.UnidadeMedida })
            .Select(grupo => new ConsumoGeneroPeriodoDto(
                grupo.Key.GeneroEstoqueId,
                grupo.Sum(consumo => consumo.Quantidade),
                grupo.Key.UnidadeMedida))
            .ToList();

        return new RelatorioConsumoDto(request.EscolaId, request.De, request.Ate, totalComensais, generos);
    }
}
