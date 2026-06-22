using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Iss;

/// <summary>Identificador forte da entidade <see cref="ItemApuracaoIss"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemApuracaoIssId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemApuracaoIssId"/>.</returns>
    public static ItemApuracaoIssId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Linha do livro eletrônico do ISS: a escrituração imutável de UMA NFS-e na apuração mensal — item
/// da lista, base, alíquota, modalidade e ISS apurado. Entidade-filha de <see cref="ApuracaoIss"/>.
/// </summary>
public sealed class ItemApuracaoIss : Entity<ItemApuracaoIssId>
{
    private ItemApuracaoIss()
    {
    }

    private ItemApuracaoIss(
        ItemApuracaoIssId id,
        ApuracaoIssId apuracaoId,
        string chaveAcesso,
        string itemListaServico,
        ValorMonetario baseCalculo,
        decimal aliquotaPercentual,
        ModalidadeIss modalidade,
        ValorMonetario issApurado)
        : base(id)
    {
        ApuracaoIssId = apuracaoId;
        ChaveAcesso = chaveAcesso;
        ItemListaServico = itemListaServico;
        BaseCalculo = baseCalculo;
        AliquotaPercentual = aliquotaPercentual;
        Modalidade = modalidade;
        IssApurado = issApurado;
    }

    /// <summary>Apuração à qual a linha pertence.</summary>
    public ApuracaoIssId ApuracaoIssId { get; private set; }

    /// <summary>Chave de acesso da NFS-e escriturada.</summary>
    public string ChaveAcesso { get; private set; } = default!;

    /// <summary>Item da lista de serviços LC 116.</summary>
    public string ItemListaServico { get; private set; } = default!;

    /// <summary>Base de cálculo (valor do serviço, R$).</summary>
    public ValorMonetario BaseCalculo { get; private set; } = default!;

    /// <summary>Alíquota aplicada (%).</summary>
    public decimal AliquotaPercentual { get; private set; }

    /// <summary>Modalidade de recolhimento (próprio/retido/substituição).</summary>
    public ModalidadeIss Modalidade { get; private set; }

    /// <summary>ISS apurado da nota (R$).</summary>
    public ValorMonetario IssApurado { get; private set; } = default!;

    /// <summary>Cria uma linha de escrituração do ISS.</summary>
    /// <param name="apuracaoId">Apuração proprietária.</param>
    /// <param name="chaveAcesso">Chave de acesso da NFS-e.</param>
    /// <param name="itemListaServico">Item da lista LC 116.</param>
    /// <param name="baseCalculo">Base de cálculo.</param>
    /// <param name="aliquotaPercentual">Alíquota aplicada (%).</param>
    /// <param name="modalidade">Modalidade de recolhimento.</param>
    /// <param name="issApurado">ISS apurado.</param>
    /// <returns>Nova <see cref="ItemApuracaoIss"/>.</returns>
    public static ItemApuracaoIss Criar(
        ApuracaoIssId apuracaoId,
        string chaveAcesso,
        string itemListaServico,
        ValorMonetario baseCalculo,
        decimal aliquotaPercentual,
        ModalidadeIss modalidade,
        ValorMonetario issApurado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaveAcesso);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemListaServico);
        ArgumentNullException.ThrowIfNull(baseCalculo);
        ArgumentNullException.ThrowIfNull(issApurado);

        return new ItemApuracaoIss(
            ItemApuracaoIssId.New(),
            apuracaoId,
            chaveAcesso.Trim(),
            itemListaServico.Trim(),
            baseCalculo,
            aliquotaPercentual,
            modalidade,
            issApurado);
    }
}
