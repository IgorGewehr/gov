using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

/// <summary>Identificador forte de uma <see cref="Garantia"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct GarantiaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="GarantiaId"/>.</returns>
    public static GarantiaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Garantia de execucao do contrato — caucao contratual limitada a 5% do valor (ate 10% em obras
/// de grande vulto; Lei 14.133/2021, art. 96 e 98). Entidade-filha do agregado <see cref="Contrato"/>.
/// </summary>
public sealed class Garantia : Entity<GarantiaId>
{
    private Garantia()
    {
    }

    private Garantia(GarantiaId id, ModalidadeGarantia modalidade, decimal percentual, ValorMonetario valor, DateOnly validadeFim)
        : base(id)
    {
        Modalidade = modalidade;
        Percentual = percentual;
        Valor = valor;
        ValidadeFim = validadeFim;
    }

    /// <summary>Modalidade da garantia.</summary>
    public ModalidadeGarantia Modalidade { get; private set; }

    /// <summary>Percentual da garantia sobre o valor do contrato.</summary>
    public decimal Percentual { get; private set; }

    /// <summary>Valor da garantia prestada.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Data-fim de validade da garantia.</summary>
    public DateOnly ValidadeFim { get; private set; }

    /// <summary>Registra uma garantia de execucao.</summary>
    /// <param name="modalidade">Modalidade da garantia.</param>
    /// <param name="percentual">Percentual sobre o valor.</param>
    /// <param name="valor">Valor prestado.</param>
    /// <param name="validadeFim">Data-fim de validade.</param>
    /// <returns>Nova <see cref="Garantia"/>.</returns>
    internal static Garantia Registrar(ModalidadeGarantia modalidade, decimal percentual, ValorMonetario valor, DateOnly validadeFim)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return new Garantia(GarantiaId.New(), modalidade, percentual, valor, validadeFim);
    }
}
