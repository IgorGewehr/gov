using System.Text.RegularExpressions;

namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>
/// Codigo da Classificacao Internacional de Atencao Primaria (CIAP-2) — usado na APS.
/// Formato: uma letra (capitulo) seguida de dois digitos (ex.: K86, R05).
/// </summary>
public readonly partial record struct Ciap
{
    private Ciap(string codigo) => Codigo = codigo;

    /// <summary>Codigo CIAP-2 normalizado (maiusculo).</summary>
    public string Codigo { get; }

    /// <summary>Cria um CIAP-2 validado.</summary>
    /// <param name="codigo">Codigo CIAP-2.</param>
    /// <returns>Instancia de <see cref="Ciap"/>.</returns>
    /// <exception cref="ArgumentException">Se o codigo for vazio ou nao casar com o formato CIAP-2.</exception>
    public static Ciap De(string codigo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        var normalizado = codigo.Trim().ToUpperInvariant();
        if (!FormatoCiap().IsMatch(normalizado))
        {
            throw new ArgumentException($"CIAP-2 invalido: '{codigo}'.", nameof(codigo));
        }

        return new Ciap(normalizado);
    }

    /// <summary>Indica se o texto informado e um CIAP-2 valido.</summary>
    /// <param name="codigo">Codigo a avaliar.</param>
    /// <returns><c>true</c> se valido.</returns>
    public static bool EhValido(string? codigo)
        => !string.IsNullOrWhiteSpace(codigo) && FormatoCiap().IsMatch(codigo.Trim().ToUpperInvariant());

    /// <inheritdoc />
    public override string ToString() => Codigo;

    [GeneratedRegex(@"^[A-Z]\d{2}$")]
    private static partial Regex FormatoCiap();
}
