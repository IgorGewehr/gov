using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>
/// Assinatura digital ICP-Brasil (NGS2) de uma evolucao/atendimento. Confere nao-repudio e,
/// em NGS2, habilita a eliminacao do papel (CFM 1.821/2007; Lei 13.787/2018).
/// </summary>
public sealed class AssinaturaDigital : ValueObject
{
    private AssinaturaDigital(string certificadoIcpBrasil, string hash, DateTimeOffset carimbo, NivelGarantia nivel)
    {
        CertificadoIcpBrasil = certificadoIcpBrasil;
        Hash = hash;
        Carimbo = carimbo;
        Nivel = nivel;
    }

    /// <summary>Identificacao do certificado ICP-Brasil utilizado.</summary>
    public string CertificadoIcpBrasil { get; } = default!;

    /// <summary>Hash criptografico da assinatura.</summary>
    public string Hash { get; } = default!;

    /// <summary>Carimbo de tempo da assinatura.</summary>
    public DateTimeOffset Carimbo { get; }

    /// <summary>Nivel de garantia da assinatura (NGS1/NGS2).</summary>
    public NivelGarantia Nivel { get; }

    /// <summary>Cria uma assinatura digital ICP-Brasil validada.</summary>
    /// <param name="certificadoIcpBrasil">Identificacao do certificado ICP-Brasil.</param>
    /// <param name="hash">Hash criptografico da assinatura.</param>
    /// <param name="carimbo">Carimbo de tempo da assinatura.</param>
    /// <param name="nivel">Nivel de garantia (NGS1/NGS2).</param>
    /// <returns>Instancia de <see cref="AssinaturaDigital"/>.</returns>
    /// <exception cref="ArgumentException">Se certificado ou hash forem vazios.</exception>
    public static AssinaturaDigital De(string certificadoIcpBrasil, string hash, DateTimeOffset carimbo, NivelGarantia nivel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificadoIcpBrasil);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return new AssinaturaDigital(certificadoIcpBrasil.Trim(), hash.Trim(), carimbo, nivel);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CertificadoIcpBrasil;
        yield return Hash;
        yield return Carimbo;
        yield return Nivel;
    }
}
