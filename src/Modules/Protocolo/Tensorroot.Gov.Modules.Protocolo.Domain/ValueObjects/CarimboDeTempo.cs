using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

/// <summary>
/// Origem do carimbo de tempo: <see cref="Act"/> = Autoridade de Carimbo do Tempo credenciada
/// ICP-Brasil (RFC 3161 / DOC-ICP-12 v2.1 — prova oponivel ao TCE); <see cref="Local"/> = relogio
/// local (fallback/dev), que NAO satisfaz ato qualificado.
/// </summary>
public enum OrigemCarimbo
{
    /// <summary>Carimbo do relogio local (fallback/dev) — sem token oponivel.</summary>
    Local = 1,

    /// <summary>Carimbo de ACT credenciada ICP-Brasil (RFC 3161) com TST validado.</summary>
    Act = 2,
}

/// <summary>
/// Carimbo de tempo: atestacao temporal confiavel do instante da assinatura (Lei 14.063/2020).
/// Toda assinatura valida o carrega (I-8).
/// <para>
/// W9.4 (Peca 1): estendido para portar o TST real (RFC 3161) — token CMS, serial, policy, algoritmo
/// e o HASH carimbado — quando emitido por uma ACT credenciada ICP-Brasil. A factory <see cref="De"/>
/// (relogio local) e mantida e produz <see cref="OrigemCarimbo.Local"/>; a nova <see cref="DeToken"/>
/// produz <see cref="OrigemCarimbo.Act"/> com a prova oponivel ao TCE.
/// </para>
/// </summary>
public sealed class CarimboDeTempo : ValueObject
{
    /// <summary>Comprimento maximo do nome da autoridade de carimbo de tempo.</summary>
    public const int ComprimentoMaximoAutoridade = 120;

    /// <summary>Comprimento maximo do serial do token (TSTInfo.serialNumber em hex/decimal).</summary>
    public const int ComprimentoMaximoSerial = 128;

    /// <summary>Comprimento maximo do OID da politica de carimbo (TSA policy).</summary>
    public const int ComprimentoMaximoPolitica = 64;

    /// <summary>Comprimento maximo do identificador do algoritmo de hash.</summary>
    public const int ComprimentoMaximoAlgoritmo = 20;

    /// <summary>Algoritmo de hash padrao do MessageImprint (RFC 3161 / ICP-Brasil).</summary>
    public const string AlgoritmoPadrao = "SHA-256";

    private CarimboDeTempo(
        DateTime instanteUtc,
        string autoridade,
        OrigemCarimbo origem,
        string algoritmoHash,
        string? hashCarimbado,
        string? tokenBase64,
        string? serialToken,
        string? politicaCarimbo)
    {
        InstanteUtc = instanteUtc;
        Autoridade = autoridade;
        Origem = origem;
        AlgoritmoHash = algoritmoHash;
        HashCarimbado = hashCarimbado;
        TokenBase64 = tokenBase64;
        SerialToken = serialToken;
        PoliticaCarimbo = politicaCarimbo;
    }

    /// <summary>Instante (UTC) atestado pela autoridade de carimbo de tempo (genTime do TSTInfo).</summary>
    public DateTime InstanteUtc { get; }

    /// <summary>Autoridade de carimbo de tempo que atestou o instante.</summary>
    public string Autoridade { get; }

    /// <summary>Origem do carimbo (ACT credenciada ou relogio local).</summary>
    public OrigemCarimbo Origem { get; }

    /// <summary>Identificador do algoritmo de hash do MessageImprint (ex.: "SHA-256").</summary>
    public string AlgoritmoHash { get; }

    /// <summary>Hash que a ACT atestou (== Hash do documento). Nulo em carimbo local.</summary>
    public string? HashCarimbado { get; }

    /// <summary>TST (CMS) DER em Base64 — a prova oponivel ao TCE. Nulo em carimbo local.</summary>
    public string? TokenBase64 { get; }

    /// <summary>serialNumber do TSTInfo. Nulo em carimbo local.</summary>
    public string? SerialToken { get; }

    /// <summary>OID da politica de carimbo (TSA policy ICP-Brasil). Nulo em carimbo local.</summary>
    public string? PoliticaCarimbo { get; }

    /// <summary>Cria um carimbo de tempo LOCAL (relogio do sistema — fallback/dev).</summary>
    /// <param name="instanteUtc">Instante (UTC) atestado.</param>
    /// <param name="autoridade">Autoridade emissora do carimbo.</param>
    /// <returns>Instancia de <see cref="CarimboDeTempo"/> com <see cref="OrigemCarimbo.Local"/>.</returns>
    /// <exception cref="ArgumentException">Se a autoridade for vazia ou exceder o limite.</exception>
    public static CarimboDeTempo De(DateTime instanteUtc, string autoridade)
    {
        var normalizado = NormalizarAutoridade(autoridade);
        return new CarimboDeTempo(
            instanteUtc,
            normalizado,
            OrigemCarimbo.Local,
            AlgoritmoPadrao,
            hashCarimbado: null,
            tokenBase64: null,
            serialToken: null,
            politicaCarimbo: null);
    }

    /// <summary>
    /// Cria um carimbo de tempo de ACT (RFC 3161) a partir do TST validado, vinculado ao hash carimbado.
    /// </summary>
    /// <param name="genTimeUtc">Instante (UTC) atestado pela ACT (genTime do TSTInfo).</param>
    /// <param name="autoridade">Nome da ACT credenciada.</param>
    /// <param name="tokenBase64">TST (CMS DER) em Base64 — prova oponivel.</param>
    /// <param name="serialToken">serialNumber do TSTInfo.</param>
    /// <param name="politica">OID da politica de carimbo.</param>
    /// <param name="hashCarimbado">Hash que a ACT atestou (deve coincidir com o do documento).</param>
    /// <param name="algoritmoHash">Identificador do algoritmo de hash (padrao "SHA-256").</param>
    /// <returns>Instancia de <see cref="CarimboDeTempo"/> com <see cref="OrigemCarimbo.Act"/>.</returns>
    /// <exception cref="ArgumentException">Se algum campo obrigatorio for vazio ou exceder limites.</exception>
    public static CarimboDeTempo DeToken(
        DateTime genTimeUtc,
        string autoridade,
        string tokenBase64,
        string serialToken,
        string politica,
        string hashCarimbado,
        string algoritmoHash = AlgoritmoPadrao)
    {
        var autoridadeNorm = NormalizarAutoridade(autoridade);
        var token = NormalizarObrigatorio(tokenBase64, nameof(tokenBase64), int.MaxValue);
        var serial = NormalizarObrigatorio(serialToken, nameof(serialToken), ComprimentoMaximoSerial);
        var politicaNorm = NormalizarObrigatorio(politica, nameof(politica), ComprimentoMaximoPolitica);
        var algoritmoNorm = NormalizarObrigatorio(algoritmoHash, nameof(algoritmoHash), ComprimentoMaximoAlgoritmo);

        ArgumentException.ThrowIfNullOrWhiteSpace(hashCarimbado);
        var hashNorm = hashCarimbado.Trim().ToLowerInvariant();
        if (hashNorm.Length != Hash.ComprimentoSha256)
        {
            throw new ArgumentException(
                $"Hash carimbado deve ter {Hash.ComprimentoSha256} caracteres hexadecimais.", nameof(hashCarimbado));
        }

        return new CarimboDeTempo(
            genTimeUtc,
            autoridadeNorm,
            OrigemCarimbo.Act,
            algoritmoNorm,
            hashNorm,
            token,
            serial,
            politicaNorm);
    }

    /// <summary>
    /// Indica se este carimbo atesta o hash informado — vinculo hash &lt;-&gt; token (I-CT2). Em carimbo
    /// local (sem token) a verificacao e vacuamente verdadeira (nao ha hash atestado a opor).
    /// </summary>
    /// <param name="hashDocumento">Hash (hex) do documento a confrontar.</param>
    /// <returns><c>true</c> se o carimbo nao tem hash atestado, ou se o hash atestado coincide.</returns>
    public bool AtestaHash(string hashDocumento)
    {
        if (HashCarimbado is null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(hashDocumento))
        {
            return false;
        }

        return string.Equals(
            HashCarimbado, hashDocumento.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override string ToString() => $"{InstanteUtc:O} ({Autoridade}; {Origem})";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return InstanteUtc;
        yield return Autoridade;
        yield return Origem;
        yield return AlgoritmoHash;
        yield return HashCarimbado;
        yield return SerialToken;
        yield return PoliticaCarimbo;
        yield return TokenBase64;
    }

    private static string NormalizarAutoridade(string autoridade)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(autoridade);
        var normalizado = autoridade.Trim();
        if (normalizado.Length > ComprimentoMaximoAutoridade)
        {
            throw new ArgumentException(
                $"Autoridade de carimbo de tempo excede {ComprimentoMaximoAutoridade} caracteres.", nameof(autoridade));
        }

        return normalizado;
    }

    private static string NormalizarObrigatorio(string valor, string nome, int comprimentoMaximo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor, nome);
        var normalizado = valor.Trim();
        if (normalizado.Length > comprimentoMaximo)
        {
            throw new ArgumentException($"'{nome}' excede {comprimentoMaximo} caracteres.", nome);
        }

        return normalizado;
    }
}
