namespace Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

/// <summary>
/// Cartao Nacional de Saude (CNS): identificador nacional univoco do usuario SUS na base
/// CADSUS. Chave de negocio do <see cref="Paciente"/>. Armazenado sem mascara (15 digitos)
/// e validado quanto ao formato e ao digito verificador (PIS/PASEP-like, modulo 11).
/// </summary>
public readonly record struct Cns
{
    /// <summary>Quantidade exata de digitos de um CNS.</summary>
    public const int Comprimento = 15;

    /// <summary>Cria um CNS a partir do valor informado (com ou sem mascara).</summary>
    /// <param name="valor">Sequencia de 15 digitos.</param>
    /// <exception cref="ArgumentException">Se o valor for vazio ou invalido (formato/DV).</exception>
    public Cns(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var digitos = ExtrairDigitos(valor);
        if (!EhValido(digitos))
        {
            throw new ArgumentException($"CNS invalido: '{valor}'.", nameof(valor));
        }

        Valor = digitos;
    }

    /// <summary>Os 15 digitos do CNS, sem mascara.</summary>
    public string Valor { get; }

    /// <summary>Indica se uma sequencia de 15 digitos e um CNS valido (formato + DV modulo 11).</summary>
    /// <param name="digitos">Sequencia somente de digitos.</param>
    /// <returns><c>true</c> se valido; caso contrario, <c>false</c>.</returns>
    public static bool EhValido(string? digitos)
    {
        if (digitos is null || digitos.Length != Comprimento)
        {
            return false;
        }

        foreach (var c in digitos)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        // Primeiro digito nao pode ser zero (faixas validas do CNS: 1, 2, 7, 8, 9).
        var inicial = digitos[0];
        if (inicial is not ('1' or '2' or '7' or '8' or '9'))
        {
            return false;
        }

        var soma = 0;
        for (var i = 0; i < Comprimento; i++)
        {
            soma += (digitos[i] - '0') * (Comprimento - i);
        }

        return soma % 11 == 0;
    }

    /// <inheritdoc />
    public override string ToString() => Valor;

    private static string ExtrairDigitos(string valor)
    {
        Span<char> buffer = stackalloc char[valor.Length];
        var escritos = 0;
        foreach (var c in valor)
        {
            if (char.IsAsciiDigit(c))
            {
                buffer[escritos++] = c;
            }
        }

        return new string(buffer[..escritos]);
    }
}
