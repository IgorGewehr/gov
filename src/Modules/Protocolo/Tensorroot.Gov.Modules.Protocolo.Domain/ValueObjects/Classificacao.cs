namespace Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

/// <summary>
/// Classe documental do processo — vincula a Tabela de Temporalidade e Destinacao
/// (TTD/CONARQ). Identifica o codigo de classificacao arquivistica do processo.
/// </summary>
public readonly record struct Classificacao
{
    /// <summary>Comprimento maximo do codigo de classificacao.</summary>
    public const int ComprimentoMaximo = 60;

    /// <summary>Cria uma classificacao validada (nao vazia e dentro do limite).</summary>
    /// <param name="codigo">Codigo de classificacao documental.</param>
    /// <exception cref="ArgumentException">Se o codigo for vazio ou exceder o comprimento maximo.</exception>
    public Classificacao(string codigo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        var normalizado = codigo.Trim();
        if (normalizado.Length > ComprimentoMaximo)
        {
            throw new ArgumentException($"Classificacao excede {ComprimentoMaximo} caracteres.", nameof(codigo));
        }

        Codigo = normalizado;
    }

    /// <summary>Codigo da classe documental.</summary>
    public string Codigo { get; }

    /// <inheritdoc />
    public override string ToString() => Codigo;
}
