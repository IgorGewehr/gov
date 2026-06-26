using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Desif;

/// <summary>Identificador forte da entidade <see cref="SubtituloDesif"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct SubtituloDesifId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="SubtituloDesifId"/>.</returns>
    public static SubtituloDesifId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Subtítulo COSIF tributável da DES-IF (Registro 0430 — Demonstrativo da apuração da receita tributável
/// e do ISSQN mensal devido por subtítulo): conta/subtítulo do Plano Contábil COSIF, código de tributação
/// DES-IF (Anexo 6), item da lista LC 116 correlato, receita tributável (base), alíquota e ISSQN devido
/// do subtítulo. Imutável após escriturado (auditoria). Entidade-filha de <see cref="DeclaracaoDesif"/>.
/// </summary>
public sealed class SubtituloDesif : Entity<SubtituloDesifId>
{
    private SubtituloDesif()
    {
    }

    private SubtituloDesif(
        SubtituloDesifId id,
        DeclaracaoDesifId declaracaoId,
        string contaCosif,
        string codigoTributacaoDesif,
        string itemListaServico,
        string descricao,
        ValorMonetario baseCalculo,
        decimal aliquotaPercentual,
        ValorMonetario issqnDevido)
        : base(id)
    {
        DeclaracaoDesifId = declaracaoId;
        ContaCosif = contaCosif;
        CodigoTributacaoDesif = codigoTributacaoDesif;
        ItemListaServico = itemListaServico;
        Descricao = descricao;
        BaseCalculo = baseCalculo;
        AliquotaPercentual = aliquotaPercentual;
        IssqnDevido = issqnDevido;
    }

    /// <summary>Declaração à qual o subtítulo pertence.</summary>
    public DeclaracaoDesifId DeclaracaoDesifId { get; private set; }

    /// <summary>Conta/subtítulo do Plano Contábil COSIF (ex.: "7.1.7.99.00-8").</summary>
    public string ContaCosif { get; private set; } = default!;

    /// <summary>Código de tributação da tabela DES-IF (Anexo 6 do modelo conceitual ABRASF).</summary>
    public string CodigoTributacaoDesif { get; private set; } = default!;

    /// <summary>Item da lista de serviços LC 116/2003 correlato (ex.: "15.01").</summary>
    public string ItemListaServico { get; private set; } = default!;

    /// <summary>Descrição do subtítulo/serviço.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Receita tributável do subtítulo (base de cálculo, R$).</summary>
    public ValorMonetario BaseCalculo { get; private set; } = default!;

    /// <summary>Alíquota aplicada (%).</summary>
    public decimal AliquotaPercentual { get; private set; }

    /// <summary>ISSQN devido do subtítulo (base × alíquota, R$).</summary>
    public ValorMonetario IssqnDevido { get; private set; } = default!;

    /// <summary>Cria um subtítulo COSIF tributável (Registro 0430).</summary>
    /// <param name="declaracaoId">Declaração proprietária.</param>
    /// <param name="contaCosif">Conta/subtítulo COSIF.</param>
    /// <param name="codigoTributacaoDesif">Código de tributação DES-IF (Anexo 6).</param>
    /// <param name="itemListaServico">Item da lista LC 116.</param>
    /// <param name="descricao">Descrição do subtítulo.</param>
    /// <param name="baseCalculo">Receita tributável (base de cálculo).</param>
    /// <param name="aliquotaPercentual">Alíquota aplicada (%).</param>
    /// <param name="issqnDevido">ISSQN devido do subtítulo.</param>
    /// <returns>Novo <see cref="SubtituloDesif"/>.</returns>
    public static SubtituloDesif Criar(
        DeclaracaoDesifId declaracaoId,
        string contaCosif,
        string codigoTributacaoDesif,
        string itemListaServico,
        string descricao,
        ValorMonetario baseCalculo,
        decimal aliquotaPercentual,
        ValorMonetario issqnDevido)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contaCosif);
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoTributacaoDesif);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemListaServico);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentNullException.ThrowIfNull(baseCalculo);
        ArgumentNullException.ThrowIfNull(issqnDevido);

        return new SubtituloDesif(
            SubtituloDesifId.New(),
            declaracaoId,
            contaCosif.Trim(),
            codigoTributacaoDesif.Trim(),
            itemListaServico.Trim(),
            descricao.Trim(),
            baseCalculo,
            aliquotaPercentual,
            issqnDevido);
    }
}
