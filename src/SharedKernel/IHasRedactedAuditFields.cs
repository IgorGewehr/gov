namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Contrato para entidades que carregam material SENSIVEL (mesmo cifrado) cujas propriedades NUNCA
/// podem ser serializadas na trilha de auditoria (AuditTrail OldValues/NewValues). O
/// AuditSaveChangesInterceptor substitui o valor dessas colunas por um marcador opaco
/// (<see cref="RedactionMarker"/>), preservando a evidencia de que a coluna mudou sem vazar o
/// conteudo. Atende A1-DESIGN §2/§7 risco 6 ("nunca serializar campos cipher") e CLAUDE.md §6.
/// </summary>
public interface IHasRedactedAuditFields
{
    /// <summary>Marcador opaco que substitui o valor de uma coluna sensivel na trilha.</summary>
    public const string RedactionMarker = "[REDACTED]";

    /// <summary>
    /// Nomes das propriedades cujo valor NUNCA deve ir para a trilha de auditoria (ex.: material
    /// cifrado do cofre A1: PfxCipher, SenhaCipher, DekWrapped, nonces e tags).
    /// </summary>
    IReadOnlySet<string> ColunasAuditoriaRedactadas { get; }
}
