using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Cofre.Domain;

/// <summary>
/// Trilha de auditoria IMUTAVEL de CADA uso do certificado para assinar (A1-DESIGN §6), alem da
/// auditoria de mutacao do <c>AuditSaveChangesInterceptor</c>. Registra sucesso E falha (toda
/// tentativa e evento auditavel) e NUNCA carrega material sensivel (.pfx/senha/DEK/chave privada) —
/// apenas o hash SHA-256 do artefato (o "o que"), quem, quando, qual cert, destino, IP e resultado.
/// Destinada ao Tribunal de Contas.
/// </summary>
public sealed class AssinaturaAuditLog : IMustHaveTenant
{
    private AssinaturaAuditLog()
    {
    }

    /// <summary>Identificador do registro de auditoria de uso.</summary>
    public Guid Id { get; private init; }

    /// <summary>Tenant (ente publico) dono do evento.</summary>
    public Guid TenantId { get; private init; }

    /// <summary>Usuario que solicitou a assinatura (claim "sub"), se autenticado.</summary>
    public string? UserId { get; private init; }

    /// <summary>Data/hora (UTC) da tentativa de assinatura.</summary>
    public DateTime TimestampUtc { get; private init; }

    /// <summary>Thumbprint do certificado usado (identificacao, nao sigiloso). Nulo se nao resolvido.</summary>
    public string? Thumbprint { get; private init; }

    /// <summary>Titular do certificado usado (nao sigiloso). Nulo se nao resolvido.</summary>
    public string? Titular { get; private init; }

    /// <summary>Finalidade/destino da assinatura (eSocial/TCE/SICONFI/Protocolo).</summary>
    public string Destino { get; private init; } = default!;

    /// <summary>Hash SHA-256 (hex) do artefato assinado — o "o que" da auditoria.</summary>
    public string? HashArtefatoSha256 { get; private init; }

    /// <summary>Identificador de correlacao da requisicao (OTel).</summary>
    public string? CorrelationId { get; private init; }

    /// <summary>Endereco IP de origem.</summary>
    public string? IpAddress { get; private init; }

    /// <summary>Verdadeiro se a assinatura teve sucesso; falso em caso de falha.</summary>
    public bool Sucesso { get; private init; }

    /// <summary>Motivo da falha (sem material sensivel). Nulo em caso de sucesso.</summary>
    public string? Motivo { get; private init; }

    /// <summary>Registra um uso BEM-SUCEDIDO do certificado para assinar.</summary>
    /// <param name="tenantId">Tenant dono do evento.</param>
    /// <param name="userId">Usuario solicitante.</param>
    /// <param name="agoraUtc">Instante (UTC) do uso.</param>
    /// <param name="thumbprint">Thumbprint do certificado usado.</param>
    /// <param name="titular">Titular do certificado usado.</param>
    /// <param name="destino">Finalidade/destino.</param>
    /// <param name="hashArtefato">Hash SHA-256 (hex) do artefato assinado.</param>
    /// <param name="correlationId">Correlacao da requisicao.</param>
    /// <param name="ip">IP de origem.</param>
    /// <returns>Registro de auditoria de sucesso.</returns>
    public static AssinaturaAuditLog Sucedido(
        Guid tenantId,
        string? userId,
        DateTime agoraUtc,
        string thumbprint,
        string titular,
        string destino,
        string hashArtefato,
        string? correlationId,
        string? ip) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            TimestampUtc = agoraUtc,
            Thumbprint = thumbprint,
            Titular = titular,
            Destino = destino,
            HashArtefatoSha256 = hashArtefato,
            CorrelationId = correlationId,
            IpAddress = ip,
            Sucesso = true,
            Motivo = null,
        };

    /// <summary>Registra uma TENTATIVA de assinatura que FALHOU (tambem e evento auditavel).</summary>
    /// <param name="tenantId">Tenant dono do evento.</param>
    /// <param name="userId">Usuario solicitante.</param>
    /// <param name="agoraUtc">Instante (UTC) da tentativa.</param>
    /// <param name="thumbprint">Thumbprint do certificado, se resolvido.</param>
    /// <param name="titular">Titular do certificado, se resolvido.</param>
    /// <param name="destino">Finalidade/destino.</param>
    /// <param name="hashArtefato">Hash SHA-256 (hex) do artefato, se calculado.</param>
    /// <param name="motivo">Motivo da falha (sem material sensivel).</param>
    /// <param name="correlationId">Correlacao da requisicao.</param>
    /// <param name="ip">IP de origem.</param>
    /// <returns>Registro de auditoria de falha.</returns>
    public static AssinaturaAuditLog Falho(
        Guid tenantId,
        string? userId,
        DateTime agoraUtc,
        string? thumbprint,
        string? titular,
        string destino,
        string? hashArtefato,
        string motivo,
        string? correlationId,
        string? ip) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            TimestampUtc = agoraUtc,
            Thumbprint = thumbprint,
            Titular = titular,
            Destino = destino,
            HashArtefatoSha256 = hashArtefato,
            CorrelationId = correlationId,
            IpAddress = ip,
            Sucesso = false,
            Motivo = motivo,
        };
}
