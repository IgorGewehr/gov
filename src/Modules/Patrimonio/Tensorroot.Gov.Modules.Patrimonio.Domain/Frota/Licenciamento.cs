using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Identificador forte de um <see cref="Licenciamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LicenciamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LicenciamentoId"/>.</returns>
    public static LicenciamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Licenciamento anual e IPVA de um veículo (CTB, Lei 9.503/1997), por exercício (ano).
/// </summary>
public sealed class Licenciamento : Entity<LicenciamentoId>
{
    private Licenciamento()
    {
    }

    private Licenciamento(
        LicenciamentoId id,
        int exercicio,
        ValorMonetario valorIpva,
        ValorMonetario valorTaxa,
        DateOnly data)
        : base(id)
    {
        Exercicio = exercicio;
        ValorIpva = valorIpva;
        ValorTaxa = valorTaxa;
        Data = data;
        Situacao = SituacaoLicenciamento.Regular;
    }

    /// <summary>Exercício (ano) do licenciamento.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Valor do IPVA do exercício.</summary>
    public ValorMonetario ValorIpva { get; private set; } = default!;

    /// <summary>Valor da taxa de licenciamento do exercício.</summary>
    public ValorMonetario ValorTaxa { get; private set; } = default!;

    /// <summary>Data do licenciamento.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Situação atual do licenciamento.</summary>
    public SituacaoLicenciamento Situacao { get; private set; }

    /// <summary>Registra um novo licenciamento regular para o exercício (I-11).</summary>
    /// <param name="exercicio">Exercício (ano).</param>
    /// <param name="valorIpva">Valor do IPVA.</param>
    /// <param name="valorTaxa">Valor da taxa de licenciamento.</param>
    /// <param name="data">Data do licenciamento.</param>
    /// <returns>Novo <see cref="Licenciamento"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o exercício for inválido.</exception>
    internal static Licenciamento Registrar(
        int exercicio,
        ValorMonetario valorIpva,
        ValorMonetario valorTaxa,
        DateOnly data)
    {
        ArgumentNullException.ThrowIfNull(valorIpva);
        ArgumentNullException.ThrowIfNull(valorTaxa);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exercicio);
        return new Licenciamento(LicenciamentoId.New(), exercicio, valorIpva, valorTaxa, data);
    }
}
