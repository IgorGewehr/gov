namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Registro IMUTÁVEL de auditoria (Audit Trail) de uma alteração de estado, gerado
/// automaticamente pelo interceptor e destinado ao escrutínio do Tribunal de Contas.
/// </summary>
public sealed class AuditTrail
{
    /// <summary>Identificador do registro de auditoria.</summary>
    public Guid Id { get; init; }

    /// <summary>Tenant (ente público) dono do registro alterado.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Nome da entidade/tabela alterada.</summary>
    public required string EntityName { get; init; }

    /// <summary>Chave primária do registro alterado (como texto invariante).</summary>
    public string? EntityId { get; init; }

    /// <summary>Operação realizada (Added, Modified, Deleted).</summary>
    public required string Action { get; init; }

    /// <summary>Valores anteriores, serializados em JSON (quando aplicável).</summary>
    public string? OldValues { get; init; }

    /// <summary>Valores novos, serializados em JSON (quando aplicável).</summary>
    public string? NewValues { get; init; }

    /// <summary>Colunas afetadas, serializadas em JSON (quando aplicável).</summary>
    public string? AffectedColumns { get; init; }

    /// <summary>Identificação do usuário responsável pela alteração.</summary>
    public string? UserId { get; init; }

    /// <summary>Endereço IP de origem da alteração.</summary>
    public string? IpAddress { get; init; }

    /// <summary>Data/hora (UTC) da alteração.</summary>
    public DateTime TimestampUtc { get; init; }
}
