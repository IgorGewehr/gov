using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

/// <summary>
/// Resumo criptografico SHA-256 do conteudo do documento (integridade — e-ARQ Brasil).
/// Hexadecimal de 64 caracteres, normalizado em minusculas; validado na criacao.
/// </summary>
public sealed class Hash : ValueObject
{
    /// <summary>Comprimento (em caracteres hexadecimais) de um digest SHA-256.</summary>
    public const int ComprimentoSha256 = 64;

    private Hash(string valor) => Valor = valor;

    /// <summary>Valor hexadecimal do digest SHA-256 (64 caracteres, minusculas).</summary>
    public string Valor { get; }

    /// <summary>Cria um <see cref="Hash"/> SHA-256 validado a partir do texto hexadecimal.</summary>
    /// <param name="valor">Digest hexadecimal de 64 caracteres.</param>
    /// <returns>Instancia de <see cref="Hash"/>.</returns>
    /// <exception cref="ArgumentException">Se o valor for vazio, nao tiver 64 caracteres ou nao for hexadecimal.</exception>
    public static Hash De(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var normalizado = valor.Trim().ToLowerInvariant();
        if (normalizado.Length != ComprimentoSha256)
        {
            throw new ArgumentException($"Hash SHA-256 deve ter {ComprimentoSha256} caracteres hexadecimais.", nameof(valor));
        }

        foreach (var caractere in normalizado)
        {
            if (!System.Uri.IsHexDigit(caractere))
            {
                throw new ArgumentException("Hash SHA-256 deve conter apenas digitos hexadecimais.", nameof(valor));
            }
        }

        return new Hash(normalizado);
    }

    /// <summary>Indica se este hash corresponde a um digest recalculado (comparacao case-insensitive).</summary>
    /// <param name="hashRecalculado">Digest hexadecimal recalculado do conteudo.</param>
    /// <returns><c>true</c> se integros (iguais).</returns>
    public bool Corresponde(string hashRecalculado)
    {
        if (string.IsNullOrWhiteSpace(hashRecalculado))
        {
            return false;
        }

        return string.Equals(Valor, hashRecalculado.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override string ToString() => Valor;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
