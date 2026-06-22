using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;

/// <summary>
/// Endereco de e-mail normalizado (minusculas, sem espacos) e sintaticamente validado.
/// Usado como identificador de login do <see cref="Usuarios.Usuario"/> dentro de um tenant.
/// </summary>
public sealed class Email : ValueObject
{
    /// <summary>Comprimento maximo aceito (RFC 5321 limita o endereco a 254 caracteres).</summary>
    public const int ComprimentoMaximo = 254;

    private Email(string valor) => Valor = valor;

    /// <summary>Endereco normalizado.</summary>
    public string Valor { get; }

    /// <summary>Cria um e-mail a partir de uma entrada bruta, normalizando e validando a sintaxe.</summary>
    /// <param name="valor">Endereco informado.</param>
    /// <returns>Instancia de <see cref="Email"/>.</returns>
    /// <exception cref="ArgumentException">Se o valor for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se exceder o comprimento maximo.</exception>
    /// <exception cref="FormatException">Se a sintaxe for invalida.</exception>
    public static Email De(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);

        var normalizado = valor.Trim().ToLower(CultureInfo.InvariantCulture);

        if (normalizado.Length > ComprimentoMaximo)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), $"E-mail excede {ComprimentoMaximo} caracteres.");
        }

        if (!EhSintaticamenteValido(normalizado))
        {
            throw new FormatException($"E-mail invalido: '{valor}'.");
        }

        return new Email(normalizado);
    }

    /// <summary>Tenta criar um e-mail sem lancar excecao.</summary>
    /// <param name="valor">Endereco informado.</param>
    /// <param name="email">E-mail resultante, quando valido.</param>
    /// <returns><c>true</c> se o valor for um e-mail valido.</returns>
    public static bool TentarCriar(string? valor, out Email? email)
    {
        email = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var normalizado = valor.Trim().ToLower(CultureInfo.InvariantCulture);
        if (normalizado.Length > ComprimentoMaximo || !EhSintaticamenteValido(normalizado))
        {
            return false;
        }

        email = new Email(normalizado);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Valor;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    private static bool EhSintaticamenteValido(string valor)
    {
        // Validacao deliberadamente conservadora: exatamente um '@', com parte local e dominio
        // nao vazios, dominio com ao menos um ponto e sem espacos. Sem regex (evita ReDoS).
        var arroba = valor.IndexOf('@', StringComparison.Ordinal);
        if (arroba <= 0 || arroba != valor.LastIndexOf('@'))
        {
            return false;
        }

        var local = valor.AsSpan(0, arroba);
        var dominio = valor.AsSpan(arroba + 1);

        if (local.IsEmpty || dominio.IsEmpty)
        {
            return false;
        }

        if (valor.Contains(' ', StringComparison.Ordinal))
        {
            return false;
        }

        var ponto = dominio.IndexOf('.');
        return ponto > 0 && ponto < dominio.Length - 1;
    }
}
