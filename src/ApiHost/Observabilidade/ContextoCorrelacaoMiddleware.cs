using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using OpenTelemetry;
using Serilog.Context;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.ApiHost.Observabilidade;

/// <summary>
/// Enricher multi-tenant dos TRÊS sinais de observabilidade (W9.7). Por requisição, resolve o
/// <c>tenant.id</c> (do JWT, via <see cref="ITenantContext"/>) e um <c>CorrelationId</c> ponta-a-ponta,
/// e os injeta em:
/// <list type="bullet">
/// <item><b>LOGS</b> — via <see cref="LogContext"/> do Serilog: toda linha logada durante a requisição
/// carrega <c>tenant.id</c> e <c>CorrelationId</c>.</item>
/// <item><b>TRACES</b> — via <see cref="Activity.SetTag"/> no span raiz (AspNetCore) e via
/// <see cref="Baggage"/>: o <see cref="EnriquecedorTenantSpanProcessor"/> propaga essas dimensões para
/// TODO span filho (incl. chamadas HttpClient às integrações governamentais).</item>
/// <item><b>MÉTRICAS</b> — o Baggage fica disponível para o enrichment de métricas da instrumentação
/// AspNetCore (callback registrado no Program), aplicando <c>tenant.id</c> como dimensão.</item>
/// </list>
/// <para>
/// O <c>CorrelationId</c> entra do cabeçalho <c>X-Correlation-Id</c> (propagação entre serviços/gateways)
/// ou é gerado quando ausente, e é ECOADO na resposta — fechando o rastro ponta-a-ponta.
/// </para>
/// <para>
/// // TODO(M10): o exportador OTLP → Azure Monitor (que envia esses sinais já enriquecidos ao backend)
/// depende de infra/credenciais de produção — fora do M9. Aqui ficam apenas os ENRICHERS.
/// </para>
/// </summary>
internal sealed class ContextoCorrelacaoMiddleware(RequestDelegate next)
{
    /// <summary>Executa o enriquecimento e segue o pipeline.</summary>
    /// <param name="context">Contexto HTTP da requisição.</param>
    /// <param name="tenantContext">Contexto do tenant (resolvido do JWT após a autenticação).</param>
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantContext);

        var correlationId = ResolverCorrelationId(context);
        var tenantId = tenantContext.HasTenant ? tenantContext.TenantId.ToString() : null;

        // TRACES (span raiz) + BAGGAGE (propaga a filhos via o span processor e a chamadas HttpClient).
        var atividade = Activity.Current;
        if (atividade is not null)
        {
            atividade.SetTag(CorrelacaoConstants.CorrelationIdKey, correlationId);
            if (tenantId is not null)
            {
                atividade.SetTag(CorrelacaoConstants.TenantIdKey, tenantId);
            }
        }

        Baggage.SetBaggage(CorrelacaoConstants.CorrelationIdKey, correlationId);
        if (tenantId is not null)
        {
            Baggage.SetBaggage(CorrelacaoConstants.TenantIdKey, tenantId);

            // MÉTRICAS — adiciona tenant.id como DIMENSÃO da métrica de requisição embutida do ASP.NET
            // Core (http.server.request.duration), via o mecanismo suportado no .NET 8
            // (IHttpMetricsTagsFeature). Cardinalidade limitada ao nº de tenants ativos — necessário
            // para SLOs/alarmes por ente público no backend.
            var metricsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
            metricsFeature?.Tags.Add(new KeyValuePair<string, object?>(CorrelacaoConstants.TenantIdKey, tenantId));
        }

        // Ecoa o CorrelationId na resposta (rastro ponta-a-ponta para o cliente/gateway).
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelacaoConstants.CorrelationHeader] = correlationId;
            return Task.CompletedTask;
        });

        // LOGS — empilha as propriedades no LogContext do Serilog por toda a requisição.
        using (LogContext.PushProperty(CorrelacaoConstants.CorrelationIdKey, correlationId))
        using (LogContext.PushProperty(CorrelacaoConstants.TenantIdKey, tenantId ?? "(sem-tenant)"))
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private static string ResolverCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelacaoConstants.CorrelationHeader, out var valor)
            && !string.IsNullOrWhiteSpace(valor))
        {
            return valor.ToString();
        }

        // Reaproveita o TraceId do span vigente quando há (mantém logs↔traces correlacionados);
        // senão gera um novo identificador.
        return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }
}
