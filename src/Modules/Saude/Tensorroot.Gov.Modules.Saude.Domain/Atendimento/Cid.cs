using System.Text.RegularExpressions;

namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>
/// Codigo da Classificacao Internacional de Doencas (CID-10) — diagnostico clinico.
/// Formato: uma letra, dois digitos e, opcionalmente, ponto seguido de um digito (ex.: J45, A09.0).
/// </summary>
public readonly partial record struct Cid
{
    private Cid(string codigo) => Codigo = codigo;

    /// <summary>Codigo CID-10 normalizado (maiusculo).</summary>
    public string Codigo { get; }

    /// <summary>Cria um CID-10 validado.</summary>
    /// <param name="codigo">Codigo CID-10.</param>
    /// <returns>Instancia de <see cref="Cid"/>.</returns>
    /// <exception cref="ArgumentException">Se o codigo for vazio ou nao casar com o formato CID-10.</exception>
    public static Cid De(string codigo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        var normalizado = codigo.Trim().ToUpperInvariant();
        if (!FormatoCid().IsMatch(normalizado))
        {
            throw new ArgumentException($"CID-10 invalido: '{codigo}'.", nameof(codigo));
        }

        return new Cid(normalizado);
    }

    /// <summary>Indica se o texto informado e um CID-10 valido.</summary>
    /// <param name="codigo">Codigo a avaliar.</param>
    /// <returns><c>true</c> se valido.</returns>
    public static bool EhValido(string? codigo)
        => !string.IsNullOrWhiteSpace(codigo) && FormatoCid().IsMatch(codigo.Trim().ToUpperInvariant());

    /// <inheritdoc />
    public override string ToString() => Codigo;

    [GeneratedRegex(@"^[A-Z]\d{2}(\.\d)?$")]
    private static partial Regex FormatoCid();
}
