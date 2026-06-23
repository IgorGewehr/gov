using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure.Seguranca;

/// <summary>
/// Implementacao de <see cref="ISenhaHasherCidadao"/> com BCrypt (mesmo algoritmo lento, salgado e
/// resistente a forca-bruta do realm interno). NUNCA armazena a senha em texto puro; a verificacao e
/// feita em tempo constante. Porta PROPRIA do modulo (sem acoplar ao interno do Identidade).
/// </summary>
public sealed class SenhaHasherCidadao : ISenhaHasherCidadao
{
    /// <summary>Work factor (custo) padrao do BCrypt. Cada +1 dobra o custo computacional.</summary>
    public const int WorkFactorPadrao = 12;

    private readonly int _workFactor;

    /// <summary>Cria o hasher com o work factor informado (ou o padrao seguro).</summary>
    /// <param name="workFactor">Custo do BCrypt (4..31). Valores fora da faixa caem no padrao.</param>
    public SenhaHasherCidadao(int workFactor = WorkFactorPadrao)
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
