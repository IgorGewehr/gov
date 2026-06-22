using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

/// <summary>
/// Endereço/localização cadastral do imóvel urbano (BCI): logradouro + número, bairro, CEP e a
/// localização fiscal (setor/quadra/lote/face de quadra e zona fiscal) que liga o imóvel à PGV.
/// Ver M6-DESIGN §1.1.
/// </summary>
public sealed class EnderecoImovel : ValueObject
{
    private EnderecoImovel(
        string logradouro,
        string? numero,
        string? complemento,
        string bairro,
        string? cep,
        string setorQuadraLote,
        string? faceQuadra,
        string zonaFiscal)
    {
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
        Bairro = bairro;
        Cep = cep;
        SetorQuadraLote = setorQuadraLote;
        FaceQuadra = faceQuadra;
        ZonaFiscal = zonaFiscal;
    }

    /// <summary>Logradouro (nome da via/rua).</summary>
    public string Logradouro { get; }

    /// <summary>Número predial, quando houver.</summary>
    public string? Numero { get; }

    /// <summary>Complemento (apto, bloco), quando houver.</summary>
    public string? Complemento { get; }

    /// <summary>Bairro.</summary>
    public string Bairro { get; }

    /// <summary>CEP (somente dígitos), quando houver.</summary>
    public string? Cep { get; }

    /// <summary>Setor/quadra/lote — código cadastral de localização na malha urbana.</summary>
    public string SetorQuadraLote { get; }

    /// <summary>Face de quadra (segmento de logradouro), base para o VUT da PGV, quando houver.</summary>
    public string? FaceQuadra { get; }

    /// <summary>
    /// Zona fiscal — chave de busca da PGV (valor do m² e fatores). É um código de lei municipal
    /// (definido na lei da PGV do município), não um valor nacional.
    /// </summary>
    public string ZonaFiscal { get; }

    /// <summary>Cria o endereço/localização cadastral.</summary>
    /// <param name="logradouro">Logradouro (obrigatório).</param>
    /// <param name="bairro">Bairro (obrigatório).</param>
    /// <param name="setorQuadraLote">Código setor/quadra/lote (obrigatório).</param>
    /// <param name="zonaFiscal">Zona fiscal da PGV (obrigatória).</param>
    /// <param name="numero">Número predial opcional.</param>
    /// <param name="complemento">Complemento opcional.</param>
    /// <param name="cep">CEP opcional.</param>
    /// <param name="faceQuadra">Face de quadra opcional.</param>
    /// <returns>Instância de <see cref="EnderecoImovel"/>.</returns>
    /// <exception cref="ArgumentException">Se um campo obrigatório for vazio.</exception>
    public static EnderecoImovel Criar(
        string logradouro,
        string bairro,
        string setorQuadraLote,
        string zonaFiscal,
        string? numero = null,
        string? complemento = null,
        string? cep = null,
        string? faceQuadra = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logradouro);
        ArgumentException.ThrowIfNullOrWhiteSpace(bairro);
        ArgumentException.ThrowIfNullOrWhiteSpace(setorQuadraLote);
        ArgumentException.ThrowIfNullOrWhiteSpace(zonaFiscal);

        return new EnderecoImovel(
            logradouro.Trim(),
            string.IsNullOrWhiteSpace(numero) ? null : numero.Trim(),
            string.IsNullOrWhiteSpace(complemento) ? null : complemento.Trim(),
            bairro.Trim(),
            string.IsNullOrWhiteSpace(cep) ? null : new string(cep.Where(char.IsDigit).ToArray()),
            setorQuadraLote.Trim(),
            string.IsNullOrWhiteSpace(faceQuadra) ? null : faceQuadra.Trim(),
            zonaFiscal.Trim());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Logradouro;
        yield return Numero;
        yield return Complemento;
        yield return Bairro;
        yield return Cep;
        yield return SetorQuadraLote;
        yield return FaceQuadra;
        yield return ZonaFiscal;
    }
}
