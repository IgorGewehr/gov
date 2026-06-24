using System.Diagnostics.Metrics;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Métricas do Outbox/DLQ (W9.7), expostas via <see cref="Meter"/> da BCL — captadas pela
/// instrumentação OpenTelemetry (o ApiHost registra o Meter <see cref="MeterName"/> no provider). Dão
/// visibilidade operacional ao backlog e ao veneno (dead-letter), pré-requisito de alarme em produção.
/// <list type="bullet">
/// <item><c>outbox.messages.published</c> (Counter) — mensagens publicadas com sucesso por drenagem.</item>
/// <item><c>outbox.messages.dead_lettered</c> (Counter) — mensagens que excederam o teto e foram para DLQ.</item>
/// <item><c>outbox.messages.pending</c> (Histogram) — backlog de pendentes elegíveis observado a cada
/// drenagem de módulo/tenant (amostra por ciclo).</item>
/// <item><c>outbox.messages.dead_lettered.total</c> (Histogram) — total acumulado de dead-letter na
/// tabela, observado a cada drenagem (estoque de veneno a inspecionar).</item>
/// </list>
/// Registrado como SINGLETON; as dimensões <c>tenant.id</c>/módulo são anexadas como tags em cada
/// medição (cardinalidade limitada ao nº de tenants × módulos licenciados).
/// </summary>
public sealed class OutboxMetrics : IDisposable
{
    /// <summary>Nome do Meter — registrado no MeterProvider do OpenTelemetry pelo ApiHost.</summary>
    public const string MeterName = "Tensorroot.Gov.Outbox";

    private readonly Meter _meter;
    private readonly Counter<long> _publicadas;
    private readonly Counter<long> _deadLettered;
    private readonly Histogram<int> _pendentes;
    private readonly Histogram<int> _deadLetteredTotal;

    /// <summary>Inicializa o Meter e seus instrumentos.</summary>
    public OutboxMetrics()
    {
        _meter = new Meter(MeterName);
        _publicadas = _meter.CreateCounter<long>(
            "outbox.messages.published", unit: "{message}", description: "Mensagens de Outbox publicadas com sucesso.");
        _deadLettered = _meter.CreateCounter<long>(
            "outbox.messages.dead_lettered", unit: "{message}", description: "Mensagens enviadas para dead-letter (veneno permanente).");
        _pendentes = _meter.CreateHistogram<int>(
            "outbox.messages.pending", unit: "{message}", description: "Backlog de mensagens pendentes elegíveis por drenagem.");
        _deadLetteredTotal = _meter.CreateHistogram<int>(
            "outbox.messages.dead_lettered.total", unit: "{message}", description: "Estoque de mensagens em dead-letter por drenagem.");
    }

    /// <summary>Registra <paramref name="quantidade"/> mensagens publicadas com sucesso para o tenant.</summary>
    public void RegistrarPublicadas(int quantidade, Guid tenantId)
    {
        if (quantidade > 0)
        {
            _publicadas.Add(quantidade, Tag(tenantId));
        }
    }

    /// <summary>Registra UMA mensagem enviada para dead-letter (chamado na transição para DLQ).</summary>
    public void RegistrarDeadLetter(Guid tenantId) => _deadLettered.Add(1, Tag(tenantId));

    /// <summary>Amostra o backlog de pendentes e o estoque de dead-letter observados numa drenagem.</summary>
    public void RegistrarSnapshot(int pendentes, int deadLetteredTotal, Guid tenantId)
    {
        _pendentes.Record(pendentes, Tag(tenantId));
        _deadLetteredTotal.Record(deadLetteredTotal, Tag(tenantId));
    }

    private static KeyValuePair<string, object?> Tag(Guid tenantId)
        => new("tenant.id", tenantId.ToString());

    /// <inheritdoc />
    public void Dispose() => _meter.Dispose();
}
