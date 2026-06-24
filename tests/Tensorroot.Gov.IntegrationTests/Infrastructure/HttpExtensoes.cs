using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tensorroot.Gov.IntegrationTests.Infrastructure;

/// <summary>Atalhos para requisicoes HTTP autenticadas (Bearer) e parsing de respostas JSON nos testes E2E.</summary>
public static class HttpExtensoes
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    /// <summary>Anexa o token Bearer ao cliente (todas as requisicoes subsequentes ficam autenticadas).</summary>
    public static HttpClient ComToken(this HttpClient cliente, string jwt)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return cliente;
    }

    /// <summary>POST com corpo JSON (objeto anonimo/DTO) — sem exigir autenticacao no chamador.</summary>
    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient cliente, string url, object corpo)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        return cliente.PostAsJsonAsync(url, corpo, OpcoesJson);
    }

    /// <summary>Le o corpo da resposta como <see cref="JsonElement"/> (parsing tolerante para asserts).</summary>
    public static async Task<JsonElement> LerJsonAsync(this HttpResponseMessage resposta)
    {
        ArgumentNullException.ThrowIfNull(resposta);
        var texto = await resposta.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(texto)
            ? default
            : JsonDocument.Parse(texto).RootElement.Clone();
    }
}
