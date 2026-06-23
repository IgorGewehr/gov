namespace Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;

/// <summary>
/// Codigo CNES (Cadastro Nacional de Estabelecimentos de Saude): identificador nacional univoco
/// de um estabelecimento de saude (7 digitos). Chave de negocio do <see cref="Estabelecimento"/>,
/// armazenado sem mascara. Validado quanto ao formato (exatamente 7 digitos numericos).
/// </summary>
public readonly record struct CodigoCnes
{
    /// <summary>Quantidade exata de digitos de um CNES.</summary>
    public const int Comprimento = 7;

    /// <summary>Cria um codigo CNES a partir do valor informado (com ou sem mascara).</summary>
    /// <param name="valor">Sequencia de 7 digitos.</param>
    /// <exception cref="ArgumentException">Se o valor for vazio ou invalido (formato).</exception>
    public CodigoCnes(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var digitos = ExtrairDigitos(valor);
        if (!EhValido(digitos))
        {
            throw new ArgumentException($"CNES invalido: '{valor}'.", nameof(valor));
        }

        Valor = digitos;
    }

    /// <summary>Os 7 digitos do CNES, sem mascara.</summary>
    public string Valor { get; }

    /// <summary>Indica se uma sequencia de 7 digitos e um CNES valido quanto ao formato.</summary>
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
