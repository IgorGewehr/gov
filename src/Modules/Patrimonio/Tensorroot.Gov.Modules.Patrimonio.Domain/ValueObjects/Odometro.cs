namespace Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

/// <summary>
/// Quilometragem acumulada de um veículo (odômetro), em quilômetros inteiros.
/// Grandeza monotônica não decrescente: só admite atualização para valor maior ou igual.
/// </summary>
/// <param name="Valor">Quilometragem acumulada (km), não-negativa.</param>
public readonly record struct Odometro(int Valor)
{
    /// <summary>Cria uma leitura de odômetro não-negativa.</summary>
    /// <param name="valor">Quilometragem em km (maior ou igual a zero).</param>
    /// <returns>Instância de <see cref="Odometro"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    public static Odometro De(int valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new Odometro(valor);
    }

    /// <summary>
    /// Avança a leitura para um novo valor, garantindo a monotonicidade não decrescente (I-3).
    /// </summary>
    /// <param name="novoValor">Nova quilometragem (km).</param>
    /// <returns>Novo <see cref="Odometro"/> com o valor informado.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o valor for inferior ao atual (odômetro retroativo).</exception>
    public Odometro Avancar(int novoValor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(novoValor);
        if (novoValor < Valor)
        {
            throw new InvalidOperationException(
                $"Odômetro não pode retroceder: atual {Valor} km, informado {novoValor} km.");
        }

        return new Odometro(novoValor);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Valor} km";
}
