namespace Tensorroot.Gov.Modules.Saude.Domain.Profissionais;

/// <summary>
/// Codigo CBO (Classificacao Brasileira de Ocupacoes): identifica a ocupacao do profissional no
/// vinculo CNES (6 digitos). Armazenado sem mascara, validado quanto ao formato.
/// </summary>
public readonly record struct Cbo
{
    /// <summary>Quantidade exata de digitos de um CBO.</summary>
    public const int Comprimento = 6;

    /// <summary>Cria um codigo CBO a partir do valor informado (com ou sem mascara).</summary>
    /// <param name="valor">Sequencia de 6 digitos.</param>
    /// <exception cref="ArgumentException">Se o valor for vazio ou invalido (formato).</exception>
    public Cbo(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var digitos = ExtrairDigitos(valor);
        if (!EhValido(digitos))
        {
            throw new ArgumentException($"CBO invalido: '{valor}'.", nameof(valor));
        }

        Valor = digitos;
    }

    /// <summary>Os 6 digitos do CBO, sem mascara.</summary>
    public string Valor { get; }

    /// <summary>Indica se uma sequencia de 6 digitos e um CBO valido quanto ao formato.</summary>
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

        return true;
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
