using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;

/// <summary>
/// Implementacao de <see cref="ISenhaHasher"/> com BCrypt (algoritmo lento, com sal embutido e
/// resistente a forca-bruta). O custo (work factor) e parametrizavel; o sal e gerado por hash.
/// NUNCA armazena a senha em texto puro. A verificacao do BCrypt e feita em tempo constante.
/// </summary>
public sealed class SenhaHasher : ISenhaHasher
{
    /// <summary>Work factor (custo) padrao do BCrypt. Cada +1 dobra o custo computacional.</summary>
    public const int WorkFactorPadrao = 12;

    private readonly int _workFactor;

    /// <summary>Cria o hasher com o work factor informado (ou o padrao seguro).</summary>
    /// <param name="workFactor">Custo do BCrypt (4..31). Valores fora da faixa caem no padrao.</param>
    public SenhaHasher(int workFactor = WorkFactorPadrao)
        => _workFactor = workFactor is >= 4 and <= 31 ? workFactor : WorkFactorPadrao;

    /// <inheritdoc />
    public string Hash(string senha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senha);
        return BCryptNet.HashPassword(senha, workFactor: _workFactor);
    }

    /// <inheritdoc />
    public bool Verificar(string senha, string hash)
    {
        if (string.IsNullOrEmpty(senha) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return BCryptNet.Verify(senha, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash malformado/legado: trata como nao-correspondencia (sem vazar a causa).
            return false;
        }
    }
}
