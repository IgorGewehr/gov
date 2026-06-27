using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;

/// <summary>
/// Posicao de um servidor (ou de uma celula da matriz) na carreira: o par CLASSE (faixa vertical,
/// avanca por promocao) e REFERENCIA (padrao/step horizontal, avanca por progressao). Ambas sao
/// posicionais (1..N), parametrizadas pela grade do plano — sem numero magico. Value Object imutavel.
/// </summary>
public sealed class PosicaoCarreira : ValueObject
{
    /// <summary>Menor indice de classe/referencia admitido (a grade inicia em 1).</summary>
    public const int IndiceMinimo = 1;

    private PosicaoCarreira(int classe, int referencia)
    {
        Classe = classe;
        Referencia = referencia;
    }

    /// <summary>Classe (faixa vertical) — avanca por promocao vertical.</summary>
    public int Classe { get; }

    /// <summary>Referencia/padrao (step horizontal) — avanca por progressao horizontal.</summary>
    public int Referencia { get; }

    /// <summary>Cria uma posicao na carreira (classe e referencia maiores ou iguais a 1).</summary>
    /// <param name="classe">Classe (vertical); maior ou igual a 1.</param>
    /// <param name="referencia">Referencia/padrao (horizontal); maior ou igual a 1.</param>
    /// <returns>Instancia de <see cref="PosicaoCarreira"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a classe ou a referencia forem menores que 1.</exception>
    public static PosicaoCarreira De(int classe, int referencia)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(classe, IndiceMinimo);
        ArgumentOutOfRangeException.ThrowIfLessThan(referencia, IndiceMinimo);
        return new PosicaoCarreira(classe, referencia);
    }

    /// <summary>Devolve a posicao avancada uma referencia (mesmo classe) — progressao horizontal.</summary>
    /// <returns>Posicao com a referencia incrementada.</returns>
    public PosicaoCarreira ProximaReferencia() => new(Classe, Referencia + 1);

    /// <summary>Devolve a posicao avancada uma classe, voltando a primeira referencia — promocao vertical.</summary>
    /// <returns>Posicao na proxima classe, referencia inicial.</returns>
    public PosicaoCarreira ProximaClasse() => new(Classe + 1, IndiceMinimo);

    /// <inheritdoc />
    public override string ToString() => $"C{Classe}-R{Referencia}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Classe;
        yield return Referencia;
    }
}
