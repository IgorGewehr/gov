using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

namespace Tensorroot.Gov.Modules.Educacao.Application.Merenda;

/// <summary>Item planejado projetado na ficha do cardapio.</summary>
/// <param name="Id">Identificador do item.</param>
/// <param name="Dia">Dia da semana (descricao).</param>
/// <param name="Refeicao">Tipo de refeicao (descricao).</param>
/// <param name="GeneroEstoqueId">Genero (ItemEstoque) por Id.</param>
/// <param name="QuantidadePerCapita">Per capita.</param>
/// <param name="UnidadeMedida">Unidade de medida.</param>
public sealed record ItemCardapioDto(
    Guid Id,
    string Dia,
    string Refeicao,
    Guid GeneroEstoqueId,
    decimal QuantidadePerCapita,
    string UnidadeMedida);

/// <summary>Ficha/projecao do cardapio semanal (+ itens planejados).</summary>
/// <param name="Id">Identificador do cardapio.</param>
/// <param name="EscolaId">Escola.</param>
/// <param name="FaixaEtaria">Faixa etaria PNAE (descricao).</param>
/// <param name="Semana">Semana (segunda-feira).</param>
/// <param name="Situacao">Situacao (descricao).</param>
/// <param name="Itens">Itens planejados.</param>
public sealed record CardapioDto(
    Guid Id,
    Guid EscolaId,
    string FaixaEtaria,
    DateOnly Semana,
    string Situacao,
    IReadOnlyList<ItemCardapioDto> Itens);

/// <summary>Consumo agregado por genero no relatorio de consumo PNAE (periodo/escola).</summary>
/// <param name="GeneroEstoqueId">Genero (ItemEstoque) por Id.</param>
/// <param name="QuantidadeTotal">Quantidade total consumida no periodo.</param>
/// <param name="UnidadeMedida">Unidade de medida.</param>
public sealed record ConsumoGeneroPeriodoDto(Guid GeneroEstoqueId, decimal QuantidadeTotal, string UnidadeMedida);

/// <summary>Relatorio de consumo de generos por escola/periodo.</summary>
/// <param name="EscolaId">Escola.</param>
/// <param name="De">Inicio do periodo.</param>
/// <param name="Ate">Fim do periodo.</param>
/// <param name="TotalComensais">Soma dos comensais atendidos no periodo.</param>
/// <param name="Generos">Consumo agregado por genero.</param>
public sealed record RelatorioConsumoDto(
    Guid EscolaId,
    DateOnly De,
    DateOnly Ate,
    int TotalComensais,
    IReadOnlyList<ConsumoGeneroPeriodoDto> Generos);

/// <summary>Mapeamentos de projecao do dominio de Merenda para DTOs.</summary>
internal static class MerendaMapeamento
{
    public static CardapioDto ParaDto(Cardapio cardapio)
        => new(
            cardapio.Id.Value,
            cardapio.EscolaId.Value,
            cardapio.FaixaEtaria.ToString(),
            cardapio.Semana,
            cardapio.Situacao.ToString(),
            cardapio.Itens.Select(ParaItemDto).ToList());

    public static ItemCardapioDto ParaItemDto(ItemCardapio item)
        => new(
            item.Id.Value,
            item.Dia.ToString(),
            item.Refeicao.ToString(),
            item.GeneroEstoqueId,
            item.QuantidadePerCapita,
            item.UnidadeMedida);
}
