using System.Globalization;
using System.Net.Http.Json;
using Tensorroot.Gov.Modules.Tributos.Application.Nfse;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Nfse;

/// <summary>
/// Gateway NFS-e de produção: consulta o Ambiente de Dados Nacional (ADN)/Receita Federal via HTTP.
/// A resiliência (retry + circuit breaker + timeout) é aplicada por <c>AddStandardResilienceHandler</c>
/// no registro do <see cref="HttpClient"/> (Polly), mantendo este gateway focado na tradução (ACL).
/// </summary>
public sealed class AdnNfseGateway(HttpClient httpClient) : INfseNacionalGateway
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<NfseDocumento>> ObterNotasEmitidasAsync(string cnpjPrestador, DateOnly desde, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpjPrestador);

        var rota = string.Create(
            CultureInfo.InvariantCulture,
            $"nfse/emitidas?cnpj={Uri.EscapeDataString(cnpjPrestador)}&desde={desde:yyyy-MM-dd}");

        var documentos = await httpClient
            .GetFromJsonAsync<NfseDocumento[]>(rota, cancellationToken)
            .ConfigureAwait(false);

        return documentos ?? [];
    }
}
