using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que representa o Numero de Identificacao Social (NIS) — chave do CadUnico —
/// composto por 11 digitos com digito verificador (modulo 11) conferido. Armazenado sem mascara.
/// Expoe versao mascarada para minimizacao no barramento (LGPD art. 11).
/// </summary>
public sealed class Nis : ValueObject
{
    private static readonly int[] Pesos = [3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    private Nis(string digitos) => Digitos = digitos;

    /// <summary>Os 11 digitos do NIS, sem mascara.</summary>
    public string Digitos { get; }

    /// <summary>Versao mascarada do NIS (apenas os 3 ultimos digitos visiveis) para o barramento.</summary>
    public string Mascarado => $"********{Digitos[8..]}";

    /// <summary>Cria um NIS a partir de uma entrada com ou sem mascara.</summary>
    /// <param name="valor">Ex.: "123.45678.91-0" ou "12345678910".</param>
    /// <returns>Instancia valida de <see cref="Nis"/>.</returns>
    /// <exception cref="ArgumentNullException">Quando a entrada e nula.</exception>
    /// <exception cref="ArgumentException">Quando o NIS e invalido.</exception>
    public static Nis Create(string valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        var digitos = ExtrairDigitos(valor);
        return IsValid(digitos)
            ? new Nis(digitos)
            : throw new ArgumentException($"NIS invalido: '{valor}'.", nameof(valor));
    }

    /// <summary>Tenta criar um NIS, sem lancar excecao quando invalido.</summary>
    /// <param name="valor">Entrada com ou sem mascara.</param>
    /// <param name="nis">NIS criado, quando valido.</param>
    /// <returns><c>true</c> se valido; caso contrario, <c>false</c>.</returns>
    public static bool TryCreate(string? valor, out Nis? nis)
    {
        nis = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var digitos = ExtrairDigitos(valor);
        if (!IsValid(digitos))
        {
            return false;
        }

        nis = new Nis(digitos);
        return true;
    }

    /// <summary>Indica se uma sequencia de 11 digitos e um NIS valido (DV modulo 11).</summary>
    /// <param name="digitos">Sequencia somente de digitos.</param>
    /// <returns><c>true</c> se valido.</returns>
    public static bool IsValid(string digitos)
    {
        ArgumentNullException.ThrowIfNull(digitos);
        if (digitos.Length != 11)
        {
            return false;
        }

        var soma = 0;
        for (var i = 0; i < 10; i++)
        {
            soma += (digitos[i] - '0') * Pesos[i];
        }

        var resto = soma % 11;
        var dv = resto < 2 ? 0 : 11 - resto;
        return digitos[10] - '0' == dv;
    }

    /// <inheritdoc />
    public override string ToString() => Digitos;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Digitos;
    }

    private static string ExtrairDigitos(string value)
    {
        Span<char> buffer = value.Length <= 32 ? stackalloc char[value.Length] : new char[value.Length];
        var count = 0;
        foreach (var c in value)
        {
            if (char.IsAsciiDigit(c))
            {
                buffer[count++] = c;
            }
        }

        return new string(buffer[..count]);
    }
}
