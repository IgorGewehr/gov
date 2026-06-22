namespace Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

/// <summary>
/// Registro Nacional de Veículos Automotores (RENAVAM, CTB Lei 9.503/1997):
/// 11 dígitos, sendo o último o dígito verificador (módulo 11).
/// </summary>
/// <param name="Digitos">RENAVAM normalizado com 11 dígitos.</param>
public readonly record struct Renavam(string Digitos)
{
    private const int Tamanho = 11;
    private static readonly int[] Pesos = [3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>Cria um RENAVAM validado quanto ao tamanho e ao dígito verificador.</summary>
    /// <param name="valor">RENAVAM informado (com ou sem máscara; até 11 dígitos).</param>
    /// <returns>Instância de <see cref="Renavam"/> com 11 dígitos.</returns>
    /// <exception cref="ArgumentException">Se o RENAVAM for inválido.</exception>
    public static Renavam Criar(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var digitos = new string(valor.Where(char.IsDigit).ToArray()).PadLeft(Tamanho, '0');
        if (!EhValido(digitos))
        {
            throw new ArgumentException($"RENAVAM inválido: '{valor}'.", nameof(valor));
        }

        return new Renavam(digitos);
    }

    /// <summary>Indica se o valor informado é um RENAVAM válido (tamanho + dígito verificador).</summary>
    /// <param name="valor">RENAVAM a verificar.</param>
    /// <returns><c>true</c> se válido.</returns>
    public static bool EhValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var digitos = new string(valor.Where(char.IsDigit).ToArray());
        if (digitos.Length is 0 or > Tamanho)
        {
            return false;
        }

        digitos = digitos.PadLeft(Tamanho, '0');
        if (digitos.All(c => c == '0'))
        {
            return false;
        }

        var soma = 0;
        for (var i = 0; i < Tamanho - 1; i++)
        {
            soma += (digitos[i] - '0') * Pesos[i];
        }

        var resto = soma % 11;
        var verificador = resto < 2 ? 0 : 11 - resto;
        return verificador == digitos[Tamanho - 1] - '0';
    }

    /// <inheritdoc />
    public override string ToString() => Digitos;
}
