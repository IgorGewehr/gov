namespace Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

/// <summary>Número (identificador único por tenant) de tombamento de um bem patrimonial.</summary>
public readonly record struct NumeroTombamento
{
    /// <summary>Comprimento máximo do número de tombo.</summary>
    public const int ComprimentoMaximo = 40;

    private NumeroTombamento(string valor) => Valor = valor;

    /// <summary>Texto do número de tombo.</summary>
    public string Valor { get; }

    /// <summary>Cria um número de tombamento validado (não vazio e dentro do limite).</summary>
    /// <param name="valor">Texto do número de tombo.</param>
    /// <returns>Instância de <see cref="NumeroTombamento"/>.</returns>
    /// <exception cref="ArgumentException">Se o valor for vazio ou exceder o comprimento máximo.</exception>
    public static NumeroTombamento De(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var normalizado = valor.Trim();
        if (normalizado.Length > ComprimentoMaximo)
        {
            throw new ArgumentException($"Número de tombamento excede {ComprimentoMaximo} caracteres.", nameof(valor));
        }

        return new NumeroTombamento(normalizado);
    }

    /// <inheritdoc />
    public override string ToString() => Valor;
}
