using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.SharedKernel.ValueObjects;

/// <summary>
/// Objeto de Valor que representa um CNPJ válido (14 dígitos com dígitos
/// verificadores conferidos). Armazenado sem máscara.
/// </summary>
public sealed class Cnpj : ValueObject
{
    private Cnpj(string digitos) => Digitos = digitos;

    /// <summary>Os 14 dígitos do CNPJ, sem máscara.</summary>
    public string Digitos { get; }

    /// <summary>Cria um CNPJ a partir de entrada com ou sem máscara.</summary>
    /// <param name="valor">Ex.: "11.222.333/0001-81" ou "11222333000181".</param>
    /// <returns>Instância válida de <see cref="Cnpj"/>.</returns>
    /// <exception cref="ArgumentException">Quando o CNPJ é inválido.</exception>
    public static Cnpj Create(string valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        var digitos = DigitoUtil.ExtractDigits(valor);
        return IsValid(digitos)
            ? new Cnpj(digitos)
            : throw new ArgumentException($"CNPJ inválido: '{valor}'.", nameof(valor));
    }

    /// <summary>Tenta criar um CNPJ, sem lançar exceção quando inválido.</summary>
    /// <param name="valor">Entrada com ou sem máscara.</param>
    /// <param name="cnpj">CNPJ criado, quando válido.</param>
    /// <returns><c>true</c> se válido; caso contrário, <c>false</c>.</returns>
    public static bool TryCreate(string? valor, out Cnpj? cnpj)
    {
        cnpj = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var digitos = DigitoUtil.ExtractDigits(valor);
        if (!IsValid(digitos))
        {
            return false;
        }

        cnpj = new Cnpj(digitos);
        return true;
    }

    /// <summary>Indica se uma sequência de 14 dígitos é um CNPJ válido.</summary>
    /// <param name="digitos">Sequência somente de dígitos.</param>
    /// <returns><c>true</c> se válido.</returns>
    public static bool IsValid(string digitos)
    {
        ArgumentNullException.ThrowIfNull(digitos);
        if (digitos.Length != 14 || DigitoUtil.AllSameDigit(digitos))
        {
            return false;
        }

        var dv1 = CalcularDigito(digitos, 12);
        var dv2 = CalcularDigito(digitos, 13);
        return digitos[12] - '0' == dv1 && digitos[13] - '0' == dv2;
    }

    /// <summary>Retorna o CNPJ com máscara (00.000.000/0000-00).</summary>
    /// <returns>CNPJ formatado.</returns>
    public string Formatar()
        => $"{Digitos[..2]}.{Digitos[2..5]}.{Digitos[5..8]}/{Digitos[8..12]}-{Digitos[12..]}";

    /// <inheritdoc />
    public override string ToString() => Digitos;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Digitos;
    }

    private static int CalcularDigito(string digitos, int tamanho)
    {
        var soma = 0;
        var peso = 2;
        for (var i = tamanho - 1; i >= 0; i--)
        {
            soma += (digitos[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
