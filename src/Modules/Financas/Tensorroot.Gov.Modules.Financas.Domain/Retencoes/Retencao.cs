using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

/// <summary>Identificador forte da entidade-filha <see cref="Retencao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RetencaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RetencaoId"/>.</returns>
    public static RetencaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Retenção/consignação apurada sobre uma liquidação: parcela do valor bruto retida do credor para
/// recolhimento a terceiro (União/INSS/Município). É entidade-filha da <c>Liquidacao</c>. O valor
/// reduz o líquido a pagar e nasce como passivo extra-orçamentário (conta a recolher), baixado quando
/// recolhido via <see cref="Recolhimentos.GuiaRecolhimento"/>.
/// </summary>
public sealed class Retencao : Entity<RetencaoId>
{
    private Retencao()
    {
    }

    private Retencao(
        RetencaoId id,
        NaturezaRetencao natureza,
        ValorMonetario valor,
        string? codigoReceita,
        decimal? aliquota,
        ValorMonetario baseCalculo,
        string? favorecidoDocumento,
        string descricao)
        : base(id)
    {
        Natureza = natureza;
        Valor = valor;
        CodigoReceita = codigoReceita;
        Aliquota = aliquota;
        BaseCalculo = baseCalculo;
        FavorecidoDocumento = favorecidoDocumento;
        Descricao = descricao;
        Recolhida = false;
    }

    /// <summary>Natureza do tributo/consignação retido.</summary>
    public NaturezaRetencao Natureza { get; private set; }

    /// <summary>Valor retido.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Código de receita (DARF/GPS/guia municipal) do recolhimento, quando aplicável.</summary>
    public string? CodigoReceita { get; private set; }

    /// <summary>Alíquota aplicada (fração), quando calculada por alíquota.</summary>
    public decimal? Aliquota { get; private set; }

    /// <summary>Base de cálculo da retenção.</summary>
    public ValorMonetario BaseCalculo { get; private set; } = default!;

    /// <summary>Documento (CNPJ/CPF) do beneficiário/recolhedor, quando aplicável.</summary>
    public string? FavorecidoDocumento { get; private set; }

    /// <summary>Descrição/histórico da retenção.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Indica se já foi recolhida (baixa do passivo extra-orçamentário).</summary>
    public bool Recolhida { get; private set; }

    /// <summary>Guia de recolhimento que baixou esta retenção, quando recolhida.</summary>
    public Guid? GuiaRecolhimentoId { get; private set; }

    /// <summary>Cria uma retenção por valor informado (ISS, caução, outras).</summary>
    /// <param name="natureza">Natureza.</param>
    /// <param name="valor">Valor retido.</param>
    /// <param name="baseCalculo">Base de cálculo.</param>
    /// <param name="descricao">Descrição.</param>
    /// <param name="codigoReceita">Código de receita (opcional).</param>
    /// <param name="aliquota">Alíquota (opcional).</param>
    /// <param name="favorecidoDocumento">Documento do favorecido (opcional).</param>
    /// <returns>Nova <see cref="Retencao"/>.</returns>
    public static Retencao Criar(
        NaturezaRetencao natureza,
        ValorMonetario valor,
        ValorMonetario baseCalculo,
        string descricao,
        string? codigoReceita = null,
        decimal? aliquota = null,
        string? favorecidoDocumento = null)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentNullException.ThrowIfNull(baseCalculo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        if (!valor.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor da retencao deve ser positivo.");
        }

        if (valor.EhMaiorQue(baseCalculo))
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Retencao nao pode exceder a base de calculo.");
        }

        return new Retencao(RetencaoId.New(), natureza, valor, codigoReceita, aliquota, baseCalculo, favorecidoDocumento, descricao.Trim());
    }

    /// <summary>Marca a retenção como recolhida, vinculando à guia que a baixou.</summary>
    /// <param name="guiaRecolhimentoId">Guia de recolhimento.</param>
    /// <exception cref="InvalidOperationException">Se já recolhida.</exception>
    public void MarcarRecolhida(Guid guiaRecolhimentoId)
    {
        if (Recolhida)
        {
            throw new InvalidOperationException("Retencao ja recolhida.");
        }

        Recolhida = true;
        GuiaRecolhimentoId = guiaRecolhimentoId;
    }

    /// <summary>Reabre a retenção (volta a pendente) ao cancelar a guia que a baixou.</summary>
    /// <param name="guiaRecolhimentoId">Guia que está sendo cancelada.</param>
    /// <exception cref="InvalidOperationException">Se a retenção não pertencer à guia informada.</exception>
    public void ReabrirRecolhimento(Guid guiaRecolhimentoId)
    {
        if (!Recolhida)
        {
            return;
        }

        if (GuiaRecolhimentoId != guiaRecolhimentoId)
        {
            throw new InvalidOperationException("Retencao vinculada a outra guia; reabertura nao permitida.");
        }

        Recolhida = false;
        GuiaRecolhimentoId = null;
    }
}
