namespace Tensorroot.Gov.ApiHost.Observabilidade;

/// <summary>
/// Nomes ÚNICOS das dimensões de correlação multi-tenant nos três sinais de observabilidade
/// (traces/metrics/logs). Centralizados (sem string mágica espalhada) para que tags de span,
/// propriedades de log e baggage usem EXATAMENTE a mesma chave — o que torna a correlação
/// cruzada (um trace → seus logs → suas métricas) confiável no backend (Azure Monitor — M10).
/// </summary>
internal static class CorrelacaoConstants
{
    /// <summary>Chave do tenant (ente público) — convenção <c>tenant.id</c> (OpenTelemetry resource/attribute).</summary>
    public const string TenantIdKey = "tenant.id";

    /// <summary>Chave do identificador de correlação ponta-a-ponta da requisição.</summary>
    public const string CorrelationIdKey = "CorrelationId";

    /// <summary>Cabeçalho HTTP de entrada/saída do identificador de correlação (propagação entre serviços).</summary>
    public const string CorrelationHeader = "X-Correlation-Id";
}
