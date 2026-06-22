using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.SharedKernel.ValueObjects;

/// <summary>
/// Objeto de Valor que representa um CPF válido (11 dígitos com dígitos
/// verificadores conferidos). Armazenado sem máscara.
/// </summary>
public sealed class Cpf : ValueObject
{
    private Cpf(string digitos) => Digitos = digitos;

    /// <summary>Os 11 dígitos do CPF, sem máscara.</summary>
    public string Digitos { get; }

    /// <summary>Cria um CPF a partir de uma entrada com ou sem máscara.</summary>
    /// <param name="valor">Ex.: "529.982.247-25" ou "52998224725".</param>
    /// <returns>Instância válida de <see cref="Cpf"/>.</returns>
    /// <exception cref="ArgumentException">Quando o CPF é inválido.</exception>
    public static Cpf Create(string valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        var digitos = DigitoUtil.ExtractDigits(valor);
        return IsValid(digitos)
            ? new Cpf(digitos)
            : throw new ArgumentException($"CPF inválido: '{valor}'.", nameof(valor));
    }

    /// <summary>Tenta criar um CPF, sem lançar exceção quando inválido.</summary>
    /// <param name="valor">Entrada com ou sem máscara.</param>
    /// <param name="cpf">CPF criado, quando válido.</param>
    /// <returns><c>true</c> se válido; caso contrário, <c>false</c>.</returns>
    public static bool TryCreate(string? valor, out Cpf? cpf)
    {
        cpf = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var digitos = DigitoUtil.ExtractDigits(valor);
        if (!IsValid(digitos))
        {
            return false;
        }

        cpf = new Cpf(digitos);
        return true;
    }

    /// <summary>Indica se uma sequência de 11 dígitos é um CPF válido.</summary>
    /// <param name="digitos">Sequência somente de dígitos.</param>
    /// <returns><c>true</c> se válido.</returns>
    public static bool IsValid(string digitos)
    {
        ArgumentNullException.ThrowIfNull(digitos);
        if (digitos.Length != 11 || DigitoUtil.AllSameDigit(digitos))
        {
            return false;
        }

        var dv1 = CalcularDigito(digitos, 9, 10);
        var dv2 = CalcularDigito(digitos, 10, 11);
        return digitos[9] - '0' == dv1 && digitos[10] - '0' == dv2;
    }

    /// <summary>Retorna o CPF com máscara (000.000.000-00).</summary>
    /// <returns>CPF formatado.</returns>
    public string Formatar() => $"{Digitos[..3]}.{Digitos[3..6]}.{Digitos[6..9]}-{Digitos[9..]}";

    /// <inheritdoc />
    public override string ToString() => Digitos;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Digitos;
    }

    private static int CalcularDigito(string digitos, int tamanho, int pesoInicial)
    {
        var soma = 0;
        for (var i = 0; i < tamanho; i++)
        {
            soma += (digitos[i] - '0') * (pesoInicial - i);
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
