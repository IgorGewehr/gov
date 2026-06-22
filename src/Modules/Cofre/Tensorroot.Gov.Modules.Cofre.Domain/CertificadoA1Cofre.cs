using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Cofre.Domain;

/// <summary>Identificador forte do agregado <see cref="CertificadoA1Cofre"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CertificadoA1CofreId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CertificadoA1CofreId"/>.</returns>
    public static CertificadoA1CofreId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Certificado digital A1 (ICP-Brasil) custodiado no cofre, no banco DEDICADO do tenant
/// (A1-DESIGN §2). O <c>.pfx</c> e a senha vivem CIFRADOS (AES-256-GCM) via envelope encryption:
/// a DEK por registro embrulha o material e a DEK e, por sua vez, embrulhada (wrapped) pela KEK do
/// Key Vault (campo <see cref="DekWrapped"/> + <see cref="KekKeyId"/>). NUNCA persiste <c>.pfx</c>
/// ou senha em claro; os campos cifrados e a DEK embrulhada JAMAIS sao expostos em DTO/endpoint/log.
/// </summary>
public sealed class CertificadoA1Cofre : AggregateRoot<CertificadoA1CofreId>, IMustHaveTenant, IHasRedactedAuditFields
{
    /// <summary>Comprimento maximo do nome do titular.</summary>
    public const int ComprimentoMaximoTitular = 200;

    /// <summary>Serie padrao do certificado custodiado server-side.</summary>
    public const string SerieA1 = "A1";

    /// <summary>
    /// Colunas de material cifrado (envelope) que NUNCA podem ser serializadas na trilha de
    /// auditoria, mesmo em Base64 cifrado (A1-DESIGN §2/§7 risco 6). O interceptor as substitui por
    /// um marcador opaco, preservando a evidencia da mutacao sem vazar o conteudo.
    /// </summary>
    private static readonly HashSet<string> ColunasRedactadas = new(StringComparer.Ordinal)
    {
        nameof(PfxCipher), nameof(PfxNonce), nameof(PfxTag),
        nameof(SenhaCipher), nameof(SenhaNonce), nameof(SenhaTag),
        nameof(DekWrapped),
    };

    private CertificadoA1Cofre()
    {
    }

    private CertificadoA1Cofre(
        CertificadoA1CofreId id,
        Guid tenantId,
        string titular,
        string cnpjTitular,
        string thumbprint,
        DateTime notBeforeUtc,
        DateTime notAfterUtc,
        MaterialCifrado pfx,
        MaterialCifrado senha,
        byte[] dekWrapped,
        string kekKeyId)
        : base(id)
    {
        TenantId = tenantId;
        Titular = titular;
        CnpjTitular = cnpjTitular;
        Thumbprint = thumbprint;
        NotBeforeUtc = notBeforeUtc;
        NotAfterUtc = notAfterUtc;
        Serie = SerieA1;
        PfxCipher = pfx.Cipher;
        PfxNonce = pfx.Nonce;
        PfxTag = pfx.Tag;
        SenhaCipher = senha.Cipher;
        SenhaNonce = senha.Nonce;
        SenhaTag = senha.Tag;
        DekWrapped = dekWrapped;
        KekKeyId = kekKeyId;
        Status = CertificadoStatus.Ativo;
        RaiseDomainEvent(new CertificadoA1Cadastrado(id.Value, tenantId, thumbprint));
    }

    /// <inheritdoc />
    public IReadOnlySet<string> ColunasAuditoriaRedactadas => ColunasRedactadas;

    /// <summary>Tenant (ente publico) dono do certificado.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>CN/Razao Social do e-CNPJ (NAO sigiloso).</summary>
    public string Titular { get; private set; } = default!;

    /// <summary>CNPJ do titular, sem mascara (NAO sigiloso).</summary>
    public string CnpjTitular { get; private set; } = default!;

    /// <summary>Impressao digital SHA-256 do certificado (identificacao, NAO sigiloso).</summary>
    public string Thumbprint { get; private set; } = default!;

    /// <summary>Inicio da validade do certificado (UTC).</summary>
    public DateTime NotBeforeUtc { get; private set; }

    /// <summary>Fim da validade do certificado (UTC).</summary>
    public DateTime NotAfterUtc { get; private set; }

    /// <summary>Serie do certificado ("A1").</summary>
    public string Serie { get; private set; } = SerieA1;

    // --- material cifrado (envelope) — NUNCA exposto/logado/serializado em DTO ---

    /// <summary>.pfx cifrado (AES-256-GCM).</summary>
    public byte[] PfxCipher { get; private set; } = default!;

    /// <summary>Nonce GCM do .pfx (12 bytes, unico por cifragem).</summary>
    public byte[] PfxNonce { get; private set; } = default!;

    /// <summary>Tag de autenticacao GCM do .pfx (16 bytes).</summary>
    public byte[] PfxTag { get; private set; } = default!;

    /// <summary>Senha do .pfx cifrada (AES-256-GCM).</summary>
    public byte[] SenhaCipher { get; private set; } = default!;

    /// <summary>Nonce GCM da senha (12 bytes, unico por cifragem).</summary>
    public byte[] SenhaNonce { get; private set; } = default!;

    /// <summary>Tag de autenticacao GCM da senha (16 bytes).</summary>
    public byte[] SenhaTag { get; private set; } = default!;

    /// <summary>DEK embrulhada (wrapped) pela KEK do Key Vault.</summary>
    public byte[] DekWrapped { get; private set; } = default!;

    /// <summary>Identificador/versao da KEK no Key Vault (para rotacao).</summary>
    public string KekKeyId { get; private set; } = default!;

    // --- ciclo de vida ---

    /// <summary>Situacao do certificado no cofre.</summary>
    public CertificadoStatus Status { get; private set; }

    /// <summary>Certificado anterior na cadeia de rotacao (se houver).</summary>
    public Guid? CertificadoAnteriorId { get; private set; }

    /// <summary>
    /// Cadastra um novo certificado A1 ATIVO no cofre, ja com o material CIFRADO (envelope). Valida
    /// as invariantes de modelagem (A1-DESIGN §2): titular preenchido, serie A1, validade futura.
    /// A cifragem/validacao da cadeia ICP-Brasil ocorre na Infrastructure ANTES desta fabrica.
    /// </summary>
    /// <param name="tenantId">Tenant dono do certificado.</param>
    /// <param name="titular">CN/Razao Social do e-CNPJ.</param>
    /// <param name="cnpjTitular">CNPJ do titular, sem mascara.</param>
    /// <param name="thumbprint">Thumbprint SHA-256 do certificado.</param>
    /// <param name="notBeforeUtc">Inicio da validade (UTC).</param>
    /// <param name="notAfterUtc">Fim da validade (UTC).</param>
    /// <param name="pfx">Bloco AES-GCM do .pfx.</param>
    /// <param name="senha">Bloco AES-GCM da senha.</param>
    /// <param name="dekWrapped">DEK embrulhada pela KEK.</param>
    /// <param name="kekKeyId">Identificador/versao da KEK.</param>
    /// <param name="agoraUtc">Instante atual (UTC) para validar a validade.</param>
    /// <returns>Novo <see cref="CertificadoA1Cofre"/> ativo.</returns>
    /// <exception cref="ArgumentException">Se titular/CNPJ/thumbprint forem vazios.</exception>
    /// <exception cref="CertificadoInvalidoException">Se a validade ja expirou.</exception>
    public static CertificadoA1Cofre Cadastrar(
        Guid tenantId,
        string titular,
        string cnpjTitular,
        string thumbprint,
        DateTime notBeforeUtc,
        DateTime notAfterUtc,
        MaterialCifrado pfx,
        MaterialCifrado senha,
        byte[] dekWrapped,
        string kekKeyId,
        DateTime agoraUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titular);
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpjTitular);
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(kekKeyId);
        ArgumentNullException.ThrowIfNull(pfx);
        ArgumentNullException.ThrowIfNull(senha);
        ArgumentNullException.ThrowIfNull(dekWrapped);

        var nome = titular.Trim();
        if (nome.Length > ComprimentoMaximoTitular)
        {
            throw new ArgumentOutOfRangeException(nameof(titular), $"Titular excede {ComprimentoMaximoTitular} caracteres.");
        }

        if (notAfterUtc <= agoraUtc)
        {
            throw new CertificadoInvalidoException("Certificado A1 expirado (NotAfter no passado).");
        }

        return new CertificadoA1Cofre(
            CertificadoA1CofreId.New(),
            tenantId,
            nome,
            cnpjTitular,
            thumbprint,
            notBeforeUtc,
            notAfterUtc,
            pfx,
            senha,
            dekWrapped,
            kekKeyId);
    }

    /// <summary>
    /// Marca este certificado como <see cref="CertificadoStatus.Substituido"/> na rotacao, apontando
    /// para o novo certificado. So um certificado ativo por (tenant, finalidade) — A1-DESIGN §2.
    /// </summary>
    /// <param name="novoCertificadoId">Identificador do novo certificado ativo.</param>
    public void MarcarSubstituido(CertificadoA1CofreId novoCertificadoId)
    {
        if (Status != CertificadoStatus.Ativo)
        {
            throw new CertificadoInvalidoException("So um certificado ativo pode ser substituido.");
        }

        Status = CertificadoStatus.Substituido;
        RaiseDomainEvent(new CertificadoA1Substituido(Id.Value, TenantId));
    }

    /// <summary>Aponta este certificado para o anterior na cadeia de rotacao.</summary>
    /// <param name="anteriorId">Identificador do certificado anterior.</param>
    public void EncadearAposRotacao(CertificadoA1CofreId anteriorId)
        => CertificadoAnteriorId = anteriorId.Value;

    /// <summary>Revoga manualmente o certificado (comprometimento/encerramento).</summary>
    public void Revogar()
    {
        if (Status is CertificadoStatus.Revogado)
        {
            return;
        }

        Status = CertificadoStatus.Revogado;
        RaiseDomainEvent(new CertificadoA1Revogado(Id.Value, TenantId));
    }

    /// <summary>Marca o certificado como expirado (NotAfter no passado).</summary>
    public void MarcarExpirado() => Status = CertificadoStatus.Expirado;
}
