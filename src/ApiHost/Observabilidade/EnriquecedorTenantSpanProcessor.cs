using System.Diagnostics;
using OpenTelemetry;

namespace Tensorroot.Gov.ApiHost.Observabilidade;

/// <summary>
/// Processador de spans do OpenTelemetry que ENRIQUECE cada Activity com as dimensões multi-tenant
/// (<c>tenant.id</c> e <c>CorrelationId</c>) lidas do <see cref="Baggage"/> do contexto corrente.
/// <para>
/// Garante que NÃO só o span raiz da requisição (já marcado pelo
/// <see cref="ContextoCorrelacaoMiddleware"/>) mas TODO span filho — chamadas <c>HttpClient</c> às
/// integrações governamentais (PNCP/Transferegov/TCE), spans de EF Core, spans internos — carregue a
/// mesma identidade de tenant/correlação. Sem isto, a observabilidade das integrações ficaria "órfã"
/// de tenant no backend, impossibilitando filtrar/alarmar por ente público.
/// </para>
/// <para>
/// Idempotente e barato: só escreve a tag quando o Baggage a tem e o span ainda não. // TODO(M10): o
/// exportador OTLP → Azure Monitor consome estes spans enriquecidos (infra/creds de produção).
/// </para>
/// </summary>
internal sealed class EnriquecedorTenantSpanProcessor : BaseProcessor<Activity>
{
    /// <inheritdoc />
    public override void OnStart(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);
        AplicarDoBaggage(data, CorrelacaoConstants.TenantIdKey);
        AplicarDoBaggage(data, CorrelacaoConstants.CorrelationIdKey);
    }

    private static void AplicarDoBaggage(Activity data, string chave)
    {
        if (data.GetTagItem(chave) is not null)
        {
            return;
        }

        var valor = Baggage.GetBaggage(chave);
        if (!string.IsNullOrEmpty(valor))
        {
            data.SetTag(chave, valor);
        }
    }
}
