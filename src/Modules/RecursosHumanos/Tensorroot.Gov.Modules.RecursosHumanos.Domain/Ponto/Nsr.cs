using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>
/// Numero Sequencial de Registro (NSR) do AFD (Portaria MTP 671/2021): inteiro positivo, estritamente
/// crescente e SEM lacunas dentro do REP (por tenant), gravado em cada linha do AFD. Aqui modelado
/// como Objeto de Valor para proteger a invariante "positivo" e padronizar a formatacao posicional.
/// // TODO(validar-oficial): largura/zero-padding exatos do campo NSR no leiaute do AFD (Anexo da 671).
/// </summary>
public sealed class Nsr : ValueObject
{
    /// <summary>Largura padrao do campo NSR no AFD (zero-padding). // TODO(validar-oficial: 9 digitos no leiaute oficial?).</summary>
    public const int LarguraPadrao = 9;

    private Nsr(long valor) => Valor = valor;

    /// <summary>Valor numerico do NSR (>= 1).</summary>
    public long Valor { get; }

    /// <summary>Primeiro NSR de um REP (1).</summary>
    /// <returns>NSR inicial.</returns>
    public static Nsr Primeiro() => new(1);

    /// <summary>Cria um NSR a partir de um valor positivo.</summary>
    /// <param name="valor">Valor numerico (>= 1).</param>
    /// <returns>Instancia valida de <see cref="Nsr"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor nao for positivo.</exception>
    public static Nsr De(long valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        return new Nsr(valor);
    }

    /// <summary>Retorna o proximo NSR (incremento de 1) — garante sequencia sem lacunas.</summary>
    /// <returns>NSR sucessor.</returns>
    public Nsr Proximo() => new(Valor + 1);

    /// <summary>Formata o NSR com zero-padding na largura posicional do AFD.</summary>
    /// <param name="largura">Largura do campo (default <see cref="LarguraPadrao"/>).</param>
    /// <returns>NSR como texto zero-preenchido.</returns>
    public string ParaPosicional(int largura = LarguraPadrao)
        => Valor.ToString(CultureInfo.InvariantCulture).PadLeft(largura, '0');

    /// <inheritdoc />
    public override string ToString() => Valor.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
