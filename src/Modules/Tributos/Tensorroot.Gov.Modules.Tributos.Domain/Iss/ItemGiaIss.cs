using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Iss;

/// <summary>Identificador forte da entidade <see cref="ItemGiaIss"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemGiaIssId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemGiaIssId"/>.</returns>
    public static ItemGiaIssId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Linha da declaração mensal de ISS (GIA): UM serviço prestado declarado pelo contribuinte — item da
/// lista LC 116, descrição, base de cálculo, alíquota, indicador de retenção na fonte e o ISS apurado
/// da linha. Imutável após declarada (auditoria). Entidade-filha de <see cref="DeclaracaoGiaIss"/>.
/// </summary>
public sealed class ItemGiaIss : Entity<ItemGiaIssId>
{
    private ItemGiaIss()
    {
    }

    private ItemGiaIss(
        ItemGiaIssId id,
        DeclaracaoGiaIssId declaracaoId,
        string itemListaServico,
        string descricao,
        ValorMonetario baseCalculo,
        decimal aliquotaPercentual,
        bool retidoNaFonte,
        ValorMonetario issApurado)
        : base(id)
    {
        DeclaracaoGiaIssId = declaracaoId;
        ItemListaServico = itemListaServico;
        Descricao = descricao;
        BaseCalculo = baseCalculo;
        AliquotaPercentual = aliquotaPercentual;
        RetidoNaFonte = retidoNaFonte;
        IssApurado = issApurado;
    }

    /// <summary>Declaração à qual a linha pertence.</summary>
    public DeclaracaoGiaIssId DeclaracaoGiaIssId { get; private set; }

    /// <summary>Item da lista de serviços LC 116/2003 (ex.: "7.02").</summary>
    public string ItemListaServico { get; private set; } = default!;

    /// <summary>Descrição do serviço prestado.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Base de cálculo (valor do serviço, R$).</summary>
    public ValorMonetario BaseCalculo { get; private set; } = default!;

    /// <summary>Alíquota aplicada (%).</summary>
    public decimal AliquotaPercentual { get; private set; }

    /// <summary>Indica que o ISS desta linha foi retido na fonte pelo tomador (não compõe o devido próprio).</summary>
    public bool RetidoNaFonte { get; private set; }

    /// <summary>ISS apurado da linha (R$).</summary>
    public ValorMonetario IssApurado { get; private set; } = default!;

    /// <summary>Cria uma linha da GIA.</summary>
    /// <param name="declaracaoId">Declaração proprietária.</param>
    /// <param name="itemListaServico">Item da lista LC 116.</param>
    /// <param name="descricao">Descrição do serviço.</param>
    /// <param name="baseCalculo">Base de cálculo.</param>
    /// <param name="aliquotaPercentual">Alíquota aplicada (%).</param>
    /// <param name="retidoNaFonte">Se o ISS foi retido na fonte.</param>
    /// <param name="issApurado">ISS apurado da linha.</param>
    /// <returns>Nova <see cref="ItemGiaIss"/>.</returns>
    public static ItemGiaIss Criar(
        DeclaracaoGiaIssId declaracaoId,
        string itemListaServico,
        string descricao,
        ValorMonetario baseCalculo,
        decimal aliquotaPercentual,
        bool retidoNaFonte,
        ValorMonetario issApurado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemListaServico);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentNullException.ThrowIfNull(baseCalculo);
        ArgumentNullException.ThrowIfNull(issApurado);

        return new ItemGiaIss(
            ItemGiaIssId.New(),
            declaracaoId,
            itemListaServico.Trim(),
            descricao.Trim(),
            baseCalculo,
            aliquotaPercentual,
            retidoNaFonte,
            issApurado);
    }
}
