using System.Globalization;
using System.Security.Cryptography;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

/// <summary>
/// Resumo criptográfico do conteúdo do pacote da remessa, verificável, que torna a remessa
/// imutável e auditável (retenção com integridade — LAI/LRF). Igualdade por valor sobre
/// (<see cref="Algoritmo"/>, <see cref="Valor"/>).
/// </summary>
public sealed class HashIntegridade : ValueObject
{
    private const string AlgoritmoPadrao = "SHA-256";

    private HashIntegridade(string algoritmo, string valor)
    {
        Algoritmo = algoritmo;
        Valor = valor;
    }

    /// <summary>Algoritmo do resumo (ex.: "SHA-256").</summary>
    public string Algoritmo { get; }

    /// <summary>Valor do resumo em hexadecimal (não vazio).</summary>
    public string Valor { get; }

    /// <summary>Cria um hash de integridade a partir de algoritmo e valor já calculados.</summary>
    /// <param name="algoritmo">Algoritmo (não vazio).</param>
    /// <param name="valor">Valor hexadecimal (não vazio).</param>
    /// <returns>Instância de <see cref="HashIntegridade"/>.</returns>
    /// <exception cref="ArgumentException">Se algoritmo ou valor forem vazios.</exception>
    public static HashIntegridade De(string algoritmo, string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(algoritmo);
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        return new HashIntegridade(algoritmo, valor.ToUpperInvariant());
    }

    /// <summary>Calcula o <see cref="HashIntegridade"/> (SHA-256) do conteúdo informado, sem alocação de cópia integral.</summary>
    /// <param name="conteudo">Conteúdo do pacote a resumir.</param>
    /// <returns>Hash de integridade calculado.</returns>
    public static HashIntegridade Calcular(ReadOnlySpan<byte> conteudo)
    {
        Span<byte> destino = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(conteudo, destino);
        return new HashIntegridade(AlgoritmoPadrao, Convert.ToHexString(destino));
    }

    /// <summary>Recomputa o resumo do conteúdo e o confere com este hash (round-trip de integridade).</summary>
    /// <param name="conteudo">Conteúdo a verificar.</param>
    /// <returns><c>true</c> se o conteúdo for íntegro (confere com este hash).</returns>
    public bool Confere(ReadOnlySpan<byte> conteudo)
    {
        Span<byte> destino = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(conteudo, destino);
        var recalculado = Convert.ToHexString(destino);
        return string.Equals(recalculado, Valor, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Algoritmo, AlgoritmoPadrao, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Algoritmo}:{Valor}");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Algoritmo;
        yield return Valor;
    }
}
