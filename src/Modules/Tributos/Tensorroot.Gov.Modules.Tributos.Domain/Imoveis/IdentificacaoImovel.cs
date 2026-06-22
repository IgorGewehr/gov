using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

/// <summary>
/// Identificação do imóvel no cadastro: a inscrição municipal (cadastral) é a chave histórica
/// sempre presente; o CIB (Cadastro Imobiliário Brasileiro — Decreto 11.208/2022) e a matrícula
/// do RGI (cartório) são opcionais. O envio ao SINTER NÃO é implementado nesta fase (piloto
/// não-capital só obrigado a partir de jan/2027). Ver M6-DESIGN §1.1.
/// </summary>
public sealed class IdentificacaoImovel : ValueObject
{
    private IdentificacaoImovel(string inscricaoMunicipal, string? cibCodigo, string? matriculaRgi)
    {
        InscricaoMunicipal = inscricaoMunicipal;
        CibCodigo = cibCodigo;
        MatriculaRgi = matriculaRgi;
    }

    /// <summary>Inscrição municipal (cadastral) — chave histórica local do IPTU, sempre presente.</summary>
    public string InscricaoMunicipal { get; }

    /// <summary>
    /// Código CIB (formato "AAAAAAA-D": 7 alfanuméricos + dígito verificador), opcional.
    /// Atribuído pela RFB via SINTER; modelado como atributo informativo. // TODO(validar-oficial):
    /// regra do dígito verificador e protocolo de envio ao SINTER pendem de manual técnico ENAT/NT CTAT 05/2025.
    /// </summary>
    public string? CibCodigo { get; }

    /// <summary>Matrícula do registro de imóveis (RGI/cartório), opcional (vínculo dominial).</summary>
    public string? MatriculaRgi { get; }

    /// <summary>Cria a identificação do imóvel.</summary>
    /// <param name="inscricaoMunicipal">Inscrição municipal cadastral (obrigatória).</param>
    /// <param name="cibCodigo">Código CIB opcional.</param>
    /// <param name="matriculaRgi">Matrícula do RGI opcional.</param>
    /// <returns>Instância de <see cref="IdentificacaoImovel"/>.</returns>
    /// <exception cref="ArgumentException">Se a inscrição municipal for vazia.</exception>
    public static IdentificacaoImovel Criar(string inscricaoMunicipal, string? cibCodigo = null, string? matriculaRgi = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inscricaoMunicipal);
        return new IdentificacaoImovel(
            inscricaoMunicipal.Trim(),
            string.IsNullOrWhiteSpace(cibCodigo) ? null : cibCodigo.Trim(),
            string.IsNullOrWhiteSpace(matriculaRgi) ? null : matriculaRgi.Trim());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return InscricaoMunicipal;
        yield return CibCodigo;
        yield return MatriculaRgi;
    }
}
