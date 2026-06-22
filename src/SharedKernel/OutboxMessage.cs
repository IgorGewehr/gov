namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Linha do padrão Outbox: persiste, na MESMA transação do estado, um evento de
/// integração para publicação posterior e confiável (consistência transacional).
/// </summary>
public sealed class OutboxMessage : IMustHaveTenant
{
    /// <summary>Identificador da mensagem.</summary>
    public Guid Id { get; init; }

    /// <summary>Tenant (ente público) dono do evento.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Nome do tipo (assembly-qualified) do evento serializado.</summary>
    public required string Type { get; init; }

    /// <summary>Conteúdo do evento serializado em JSON.</summary>
    public required string Content { get; init; }

    /// <summary>Data/hora (UTC) em que o evento ocorreu.</summary>
    public DateTime OccurredOnUtc { get; init; }

    /// <summary>Data/hora (UTC) do processamento; nulo enquanto pendente.</summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>Mensagem de erro do último processamento, quando houver.</summary>
    public string? Error { get; set; }

    /// <summary>
    /// Número de tentativas de processamento já feitas. Cada falha incrementa o contador e agenda
    /// a próxima tentativa com backoff (<see cref="NextAttemptUtc"/>). Ao atingir o teto, a mensagem
    /// vira dead-letter (<see cref="DeadLetteredOnUtc"/>) e não é mais reprocessada.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Data/hora (UTC) a partir da qual a mensagem pode ser tentada novamente (backoff exponencial).
    /// O drain seleciona apenas mensagens com <c>NextAttemptUtc &lt;= agora</c> — uma poison message
    /// recém-falhada fica adiada e não bloqueia a cabeça do lote das mensagens válidas. Nulo = elegível
    /// imediatamente (mensagem nova nunca tentada).
    /// </summary>
    public DateTime? NextAttemptUtc { get; set; }

    /// <summary>
    /// Data/hora (UTC) em que a mensagem foi enviada para dead-letter após exceder o teto de tentativas.
    /// Quando preenchida, a mensagem é DEFINITIVAMENTE ignorada pelo drain (não reprocessa, não bloqueia).
    /// </summary>
    public DateTime? DeadLetteredOnUtc { get; set; }

    /// <summary>
    /// Teto de tentativas antes do dead-letter. Constante de domínio (não mágica): após esta quantidade
    /// de falhas a mensagem é considerada veneno permanente (envelope corrompido, tipo inexistente, etc.).
    /// </summary>
    public const int MaxAttempts = 5;
}
