using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Cliente real (Anti-Corruption Layer) da API de Dados Abertos do SICONFI (STN). SOMENTE CONSULTA — não
/// há upload/envio. <c>https://apidatalake.tesouro.gov.br/ords/siconfi/tt/</c> (JSON, sem auth, rate limit
/// ~1 req/s, paginação 5.000). A resiliência (timeout/retry/circuit breaker) é aplicada por
/// <c>AddStandardResilienceHandler</c> (Polly) no registro do <see cref="HttpClient"/>. Usado para
/// RECONCILIAR e AUDITAR o status de entrega.
/// </summary>
/// <remarks>
/// Os nomes EXATOS de campos/colunas das respostas seguem o Swagger do exercício —
/// <c>// TODO(validar-leiaute-MT-2026)</c> para os atributos precisos de cada demonstrativo.
/// </remarks>
public sealed class ConsultaSiconfiHttp(HttpClient httpClient) : IConsultaSiconfi
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ValorPublicadoSiconfi>> ConsultarValoresAsync(
        ConsultaSiconfiCriterio criterio,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criterio);

        var rota = MontarRotaDemonstrativo(criterio);
        var resposta = await httpClient
            .GetFromJsonAsync<RespostaSiconfi<ItemValorSiconfi>>(rota, cancellationToken)
            .ConfigureAwait(false);

        return resposta?.Items
            .Select(item => new ValorPublicadoSiconfi(
                item.Conta ?? string.Empty,
                item.Coluna ?? string.Empty,
                item.Valor ?? 0m))
            .ToList() ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntregaSiconfi>> ConsultarEntregasAsync(
        string idEnte,
        int exercicio,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idEnte);

        var rota = string.Create(
            CultureInfo.InvariantCulture,
            $"extrato_entregas?id_ente={Uri.EscapeDataString(idEnte)}&an_referencia={exercicio}");

        var resposta = await httpClient
            .GetFromJsonAsync<RespostaSiconfi<ItemEntregaSiconfi>>(rota, cancellationToken)
            .ConfigureAwait(false);

        return resposta?.Items
            .Select(item => new EntregaSiconfi(
                item.TipoRelatorio ?? string.Empty,
                item.Periodo ?? string.Empty,
                item.Status ?? string.Empty,
                item.DataStatus))
            .ToList() ?? [];
    }

    private static string MontarRotaDemonstrativo(ConsultaSiconfiCriterio criterio)
    {
        var recurso = criterio.TipoDemonstrativo switch
        {
            DemonstrativoSiconfi.Rreo => "rreo",
            DemonstrativoSiconfi.Rgf => "rgf",
            DemonstrativoSiconfi.Dca => "dca",
            DemonstrativoSiconfi.Msc => "msc_patrimonial",
            _ => "rreo",
        };

        var rota = string.Create(
            CultureInfo.InvariantCulture,
            $"{recurso}?id_ente={Uri.EscapeDataString(criterio.IdEnte)}&an_exercicio={criterio.Exercicio}");

        if (criterio.Periodo is { } periodo)
        {
            rota += string.Create(CultureInfo.InvariantCulture, $"&nr_periodo={periodo}");
        }

        if (!string.IsNullOrWhiteSpace(criterio.CoPoder))
        {
            rota += $"&co_poder={Uri.EscapeDataString(criterio.CoPoder)}";
        }

        return rota;
    }

    private sealed record RespostaSiconfi<T>(
        [property: JsonPropertyName("items")] IReadOnlyList<T> Items);

    private sealed record ItemValorSiconfi(
        [property: JsonPropertyName("conta")] string? Conta,
        [property: JsonPropertyName("coluna")] string? Coluna,
        [property: JsonPropertyName("valor")] decimal? Valor);

    private sealed record ItemEntregaSiconfi(
        [property: JsonPropertyName("tipo_relatorio")] string? TipoRelatorio,
        [property: JsonPropertyName("periodo")] string? Periodo,
        [property: JsonPropertyName("status_relatorio")] string? Status,
        [property: JsonPropertyName("data_status")] DateOnly? DataStatus);
}
