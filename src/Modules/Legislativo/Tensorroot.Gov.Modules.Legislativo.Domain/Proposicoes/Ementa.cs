namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Resumo do objeto (ementa) de uma proposicao legislativa.</summary>
public readonly record struct Ementa
{
    /// <summary>Comprimento maximo do texto da ementa.</summary>
    public const int ComprimentoMaximo = 1000;

    private Ementa(string valor) => Valor = valor;

    /// <summary>Texto da ementa.</summary>
    public string Valor { get; }

    /// <summary>Cria uma ementa validada (nao vazia e dentro do limite).</summary>
    /// <param name="valor">Texto da ementa.</param>
    /// <returns>Instancia de <see cref="Ementa"/>.</returns>
    /// <exception cref="ArgumentException">Se o valor for vazio ou exceder o comprimento maximo.</exception>
    public static Ementa De(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var normalizado = valor.Trim();
        if (normalizado.Length > ComprimentoMaximo)
        {
            throw new ArgumentException($"Ementa excede {ComprimentoMaximo} caracteres.", nameof(valor));
        }

        return new Ementa(normalizado);
    }

    /// <inheritdoc />
    public override string ToString() => Valor;
}
