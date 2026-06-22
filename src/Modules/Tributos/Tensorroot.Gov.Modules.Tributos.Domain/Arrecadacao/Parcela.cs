using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;

/// <summary>Identificador forte da entidade <see cref="Parcela"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ParcelaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ParcelaId"/>.</returns>
    public static ParcelaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Parcela de um <see cref="Dam"/> (cota/prestação): número, valor e vencimento; controla o
/// pagamento. Entidade-filha do agregado de arrecadação.
/// </summary>
public sealed class Parcela : Entity<ParcelaId>
{
    private Parcela()
    {
    }

    private Parcela(ParcelaId id, DamId damId, int numero, ValorMonetario valor, DateOnly vencimento)
        : base(id)
    {
        DamId = damId;
        Numero = numero;
        Valor = valor;
        Vencimento = vencimento;
        Paga = false;
    }

    /// <summary>DAM ao qual pertence.</summary>
    public DamId DamId { get; private set; }

    /// <summary>Número da parcela (1..N).</summary>
    public int Numero { get; private set; }

    /// <summary>Valor da parcela.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Vencimento.</summary>
    public DateOnly Vencimento { get; private set; }

    /// <summary>Indica se a parcela está paga.</summary>
    public bool Paga { get; private set; }

    /// <summary>Data do pagamento, quando paga.</summary>
    public DateOnly? DataPagamento { get; private set; }

    /// <summary>Cria uma parcela.</summary>
    /// <param name="damId">DAM dono.</param>
    /// <param name="numero">Número (≥ 1).</param>
    /// <param name="valor">Valor da parcela.</param>
    /// <param name="vencimento">Vencimento.</param>
    /// <returns>Nova <see cref="Parcela"/>.</returns>
    public static Parcela Criar(DamId damId, int numero, ValorMonetario valor, DateOnly vencimento)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentOutOfRangeException.ThrowIfLessThan(numero, 1);
        return new Parcela(ParcelaId.New(), damId, numero, valor, vencimento);
    }

    /// <summary>Registra o pagamento da parcela.</summary>
    /// <param name="dataPagamento">Data do pagamento.</param>
    /// <exception cref="InvalidOperationException">Se a parcela já estiver paga.</exception>
    public void RegistrarPagamento(DateOnly dataPagamento)
    {
        if (Paga)
        {
            throw new InvalidOperationException($"A parcela {Numero} já está paga.");
        }

        Paga = true;
        DataPagamento = dataPagamento;
    }
}
