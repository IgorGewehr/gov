namespace Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

/// <summary>
/// Horas de uso acumuladas de um veículo/equipamento (horímetro), em horas decimais.
/// Grandeza monotônica não decrescente: só admite atualização para valor maior ou igual.
/// </summary>
/// <param name="Valor">Horas de uso acumuladas, não-negativas.</param>
public readonly record struct Horimetro(decimal Valor)
{
    /// <summary>Cria uma leitura de horímetro não-negativa.</summary>
    /// <param name="valor">Horas de uso (maior ou igual a zero).</param>
    /// <returns>Instância de <see cref="Horimetro"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    public static Horimetro De(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new Horimetro(decimal.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Avança a leitura para um novo valor, garantindo a monotonicidade não decrescente (I-4).
    /// </summary>
    /// <param name="novoValor">Novas horas de uso acumuladas.</param>
    /// <returns>Novo <see cref="Horimetro"/> com o valor informado.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o valor for inferior ao atual (horímetro retroativo).</exception>
    public Horimetro Avancar(decimal novoValor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(novoValor);
        var arredondado = decimal.Round(novoValor, 2, MidpointRounding.AwayFromZero);
        if (arredondado < Valor)
        {
            throw new InvalidOperationException(
                $"Horímetro não pode retroceder: atual {Valor} h, informado {arredondado} h.");
        }

        return new Horimetro(arredondado);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Valor} h";
}
