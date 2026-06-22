using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Identificador forte de um <see cref="Abastecimento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AbastecimentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AbastecimentoId"/>.</returns>
    public static AbastecimentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Registro de saída de combustível de um veículo da frota, sob cota por veículo,
/// com as leituras de odômetro/horímetro no ato do abastecimento.
/// </summary>
public sealed class Abastecimento : Entity<AbastecimentoId>
{
    private Abastecimento()
    {
    }

    private Abastecimento(
        AbastecimentoId id,
        DateOnly data,
        decimal litros,
        ValorMonetario valor,
        Odometro odometro,
        Horimetro horimetro,
        Guid? motoristaId)
        : base(id)
    {
        Data = data;
        Litros = litros;
        Valor = valor;
        Odometro = odometro;
        Horimetro = horimetro;
        MotoristaId = motoristaId;
    }

    /// <summary>Data do abastecimento.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Litros abastecidos.</summary>
    public decimal Litros { get; private set; }

    /// <summary>Valor do abastecimento.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Leitura do odômetro no ato.</summary>
    public Odometro Odometro { get; private set; }

    /// <summary>Leitura do horímetro no ato.</summary>
    public Horimetro Horimetro { get; private set; }

    /// <summary>Motorista que realizou o abastecimento, quando informado.</summary>
    public Guid? MotoristaId { get; private set; }

    /// <summary>Registra um novo abastecimento.</summary>
    /// <param name="data">Data do abastecimento.</param>
    /// <param name="litros">Litros abastecidos (positivo).</param>
    /// <param name="valor">Valor do abastecimento.</param>
    /// <param name="odometro">Leitura do odômetro no ato.</param>
    /// <param name="horimetro">Leitura do horímetro no ato.</param>
    /// <param name="motoristaId">Motorista, quando informado.</param>
    /// <returns>Novo <see cref="Abastecimento"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se os litros não forem positivos.</exception>
    internal static Abastecimento Registrar(
        DateOnly data,
        decimal litros,
        ValorMonetario valor,
        Odometro odometro,
        Horimetro horimetro,
        Guid? motoristaId)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(litros);
        return new Abastecimento(AbastecimentoId.New(), data, litros, valor, odometro, horimetro, motoristaId);
    }
}
