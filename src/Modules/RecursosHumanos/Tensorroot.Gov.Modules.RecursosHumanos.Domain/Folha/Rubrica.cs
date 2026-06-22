using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

/// <summary>
/// Rubrica que classifica um provento/desconto e suas incidencias na folha (tabela eSocial S-1010).
/// Deve existir e estar vigente em S-1010 na competencia (validacao na Application/Infra).
/// </summary>
public sealed class Rubrica : ValueObject
{
    private Rubrica(string codigo) => Codigo = codigo;

    /// <summary>Codigo da rubrica (referencia a S-1010), no maximo 30 caracteres.</summary>
    public string Codigo { get; }

    /// <summary>Cria uma rubrica a partir do codigo.</summary>
    /// <param name="codigo">Codigo da rubrica (nao vazio, ate 30 caracteres).</param>
    /// <returns>Instancia de <see cref="Rubrica"/>.</returns>
    /// <exception cref="ArgumentException">Se o codigo for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o codigo exceder 30 caracteres.</exception>
    public static Rubrica De(string codigo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        var normalizado = codigo.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(normalizado.Length, 30);
        return new Rubrica(normalizado);
    }

    /// <inheritdoc />
    public override string ToString() => Codigo;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Codigo;
    }
}
