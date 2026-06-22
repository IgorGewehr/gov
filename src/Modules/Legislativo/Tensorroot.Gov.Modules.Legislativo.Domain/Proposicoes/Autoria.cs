namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Autoria (iniciativa) de uma proposicao legislativa — autor(es) da materia.</summary>
public readonly record struct Autoria
{
    /// <summary>Comprimento maximo da descricao de autoria.</summary>
    public const int ComprimentoMaximo = 400;

    private Autoria(string valor) => Valor = valor;

    /// <summary>Descricao da iniciativa/autor(es).</summary>
    public string Valor { get; }

    /// <summary>Cria uma autoria validada (nao vazia e dentro do limite).</summary>
    /// <param name="valor">Descricao da iniciativa.</param>
    /// <returns>Instancia de <see cref="Autoria"/>.</returns>
    /// <exception cref="ArgumentException">Se o valor for vazio ou exceder o comprimento maximo.</exception>
    public static Autoria De(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var normalizado = valor.Trim();
        if (normalizado.Length > ComprimentoMaximo)
        {
            throw new ArgumentException($"Autoria excede {ComprimentoMaximo} caracteres.", nameof(valor));
        }

        return new Autoria(normalizado);
    }

    /// <inheritdoc />
    public override string ToString() => Valor;
}
