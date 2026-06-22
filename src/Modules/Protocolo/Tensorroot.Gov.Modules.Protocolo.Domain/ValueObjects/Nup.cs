namespace Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

/// <summary>
/// Numero Unico de Protocolo (NUP) — identificador imutavel e unico do processo
/// apos a autuacao (Decreto 8.539/2015). Validado na autuacao; unico por tenant.
/// </summary>
public readonly record struct Nup
{
    /// <summary>Comprimento maximo do NUP.</summary>
    public const int ComprimentoMaximo = 30;

    /// <summary>Cria um NUP validado (nao vazio e dentro do limite).</summary>
    /// <param name="valor">Texto do Numero Unico de Protocolo.</param>
    /// <exception cref="ArgumentException">Se o valor for vazio ou exceder o comprimento maximo.</exception>
    public Nup(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var normalizado = valor.Trim();
        if (normalizado.Length > ComprimentoMaximo)
        {
            throw new ArgumentException($"NUP excede {ComprimentoMaximo} caracteres.", nameof(valor));
        }

        Valor = normalizado;
    }

    /// <summary>Texto do Numero Unico de Protocolo.</summary>
    public string Valor { get; }

    /// <inheritdoc />
    public override string ToString() => Valor;
}
