using System.Text.RegularExpressions;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

/// <summary>
/// Placa de identificação oficial de um veículo (CTB, Lei 9.503/1997),
/// aceitando o formato antigo (AAA9999) e o padrão Mercosul (AAA9A99).
/// </summary>
/// <param name="Valor">Placa normalizada (maiúscula, sem hífen).</param>
public readonly partial record struct Placa(string Valor)
{
    [GeneratedRegex("^[A-Z]{3}[0-9][0-9A-Z][0-9]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex FormatoPlaca();

    /// <summary>Cria uma placa validada quanto ao formato (antigo ou Mercosul).</summary>
    /// <param name="valor">Placa informada (com ou sem hífen, qualquer caixa).</param>
    /// <returns>Instância de <see cref="Placa"/> normalizada.</returns>
    /// <exception cref="ArgumentException">Se a placa for vazia ou de formato inválido.</exception>
    public static Placa Criar(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var normalizada = valor.Replace("-", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();
        if (!FormatoPlaca().IsMatch(normalizada))
        {
            throw new ArgumentException($"Placa inválida: '{valor}'.", nameof(valor));
        }

        return new Placa(normalizada);
    }

    /// <summary>Indica se o valor informado é uma placa de formato válido.</summary>
    /// <param name="valor">Placa a verificar.</param>
    /// <returns><c>true</c> se válida.</returns>
    public static bool EhValida(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var normalizada = valor.Replace("-", string.Empty, StringComparison.Ordinal).Trim().ToUpperInvariant();
        return FormatoPlaca().IsMatch(normalizada);
    }

    /// <inheritdoc />
    public override string ToString() => Valor;
}
