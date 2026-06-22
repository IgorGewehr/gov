using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

/// <summary>
/// Especificação versionada de layout da remessa ao TCE-RS (SIAPC/PAD), conforme a
/// Resolução TCE-RS vigente. Igualdade por valor sobre (<see cref="Codigo"/>, <see cref="Versao"/>).
/// </summary>
public sealed class Leiaute : ValueObject
{
    private Leiaute(string codigo, string versao)
    {
        Codigo = codigo;
        Versao = versao;
    }

    /// <summary>Código do leiaute (ex.: "SIAPC"), não vazio.</summary>
    public string Codigo { get; }

    /// <summary>Versão do leiaute (ex.: "2026.1"), não vazia.</summary>
    public string Versao { get; }

    /// <summary>Cria um leiaute versionado válido.</summary>
    /// <param name="codigo">Código do leiaute (não vazio).</param>
    /// <param name="versao">Versão do leiaute (não vazia).</param>
    /// <returns>Instância de <see cref="Leiaute"/>.</returns>
    /// <exception cref="ArgumentException">Se código ou versão forem vazios.</exception>
    public static Leiaute De(string codigo, string versao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(versao);
        return new Leiaute(codigo, versao);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Codigo} {Versao}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Codigo;
        yield return Versao;
    }
}
