using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

/// <summary>Tipo do documento que comprova o direito do credor (liquidação).</summary>
public enum TipoDocumentoComprobatorio
{
    /// <summary>Nota fiscal (modelo tradicional).</summary>
    NotaFiscal = 1,

    /// <summary>NFS-e identificada pela chave de acesso (44 dígitos).</summary>
    NfseChaveAcesso = 2,

    /// <summary>Fatura.</summary>
    Fatura = 3,

    /// <summary>Recibo.</summary>
    Recibo = 4,

    /// <summary>Outro documento hábil.</summary>
    Outro = 5,
}

/// <summary>
/// Documento comprobatório que dá liquidez ao direito do credor (Lei 4.320/64, art. 63).
/// Para NFS-e guarda a chave de acesso de 44 dígitos (originada no ADN/Tributos).
/// </summary>
public sealed class DocumentoComprobatorio : ValueObject
{
    /// <summary>Comprimento da chave de acesso da NFS-e.</summary>
    public const int TamanhoChaveNfse = 44;

    private DocumentoComprobatorio(
        TipoDocumentoComprobatorio tipo,
        string? numero,
        string? chaveAcessoNfse,
        DateOnly? dataEmissao)
    {
        Tipo = tipo;
        Numero = numero;
        ChaveAcessoNfse = chaveAcessoNfse;
        DataEmissao = dataEmissao;
    }

    /// <summary>Tipo do documento.</summary>
    public TipoDocumentoComprobatorio Tipo { get; private set; }

    /// <summary>Número do documento (quando aplicável).</summary>
    public string? Numero { get; private set; }

    /// <summary>Chave de acesso da NFS-e (44 dígitos), quando aplicável.</summary>
    public string? ChaveAcessoNfse { get; private set; }

    /// <summary>Data de emissão do documento.</summary>
    public DateOnly? DataEmissao { get; private set; }

    /// <summary>Cria a partir de uma NFS-e (chave de acesso de 44 dígitos).</summary>
    /// <param name="chave44">Chave de acesso da NFS-e.</param>
    /// <param name="dataEmissao">Data de emissão.</param>
    /// <returns>Instância de <see cref="DocumentoComprobatorio"/>.</returns>
    /// <exception cref="ArgumentException">Se a chave não tiver 44 dígitos.</exception>
    public static DocumentoComprobatorio NfsE(string chave44, DateOnly? dataEmissao = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave44);
        var normalizada = chave44.Trim();
        if (normalizada.Length != TamanhoChaveNfse || !normalizada.All(char.IsDigit))
        {
            throw new ArgumentException($"A chave de acesso da NFS-e deve ter {TamanhoChaveNfse} digitos.", nameof(chave44));
        }

        return new DocumentoComprobatorio(TipoDocumentoComprobatorio.NfseChaveAcesso, null, normalizada, dataEmissao);
    }

    /// <summary>Cria a partir de uma nota fiscal tradicional.</summary>
    /// <param name="numero">Número da nota.</param>
    /// <param name="dataEmissao">Data de emissão.</param>
    /// <returns>Instância de <see cref="DocumentoComprobatorio"/>.</returns>
    public static DocumentoComprobatorio NotaFiscal(string numero, DateOnly dataEmissao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        return new DocumentoComprobatorio(TipoDocumentoComprobatorio.NotaFiscal, numero.Trim(), null, dataEmissao);
    }

    /// <summary>Cria um documento genérico (fatura, recibo ou outro).</summary>
    /// <param name="tipo">Tipo do documento.</param>
    /// <param name="numero">Número do documento.</param>
    /// <param name="dataEmissao">Data de emissão.</param>
    /// <returns>Instância de <see cref="DocumentoComprobatorio"/>.</returns>
    public static DocumentoComprobatorio Generico(TipoDocumentoComprobatorio tipo, string? numero, DateOnly? dataEmissao)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException("Tipo de documento invalido.", nameof(tipo));
        }

        if (tipo == TipoDocumentoComprobatorio.NfseChaveAcesso)
        {
            throw new ArgumentException("Use NfsE para documento do tipo NFS-e.", nameof(tipo));
        }

        return new DocumentoComprobatorio(tipo, numero?.Trim(), null, dataEmissao);
    }

    /// <inheritdoc />
    public override string ToString()
        => Tipo == TipoDocumentoComprobatorio.NfseChaveAcesso ? $"NFS-e {ChaveAcessoNfse}" : $"{Tipo} {Numero}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Tipo;
        yield return Numero;
        yield return ChaveAcessoNfse;
        yield return DataEmissao;
    }
}
