using Tensorroot.Gov.Modules.Tributos.Application.Nfse;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Nfse;

/// <summary>
/// Gateway NFS-e simulado (dev/demonstração): retorna notas determinísticas por CNPJ,
/// permitindo exercitar a sincronização e a deduplicação sem o ambiente real do ADN.
/// </summary>
public sealed class SimuladoNfseGateway : INfseNacionalGateway
{
    /// <inheritdoc />
    public Task<IReadOnlyList<NfseDocumento>> ObterNotasEmitidasAsync(string cnpjPrestador, DateOnly desde, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpjPrestador);

        IReadOnlyList<NfseDocumento> notas =
        [
            new($"NFSE-{cnpjPrestador}-0001", cnpjPrestador, null, 1000.00m, 50.00m, desde, desde.Year, desde.Month),
            new($"NFSE-{cnpjPrestador}-0002", cnpjPrestador, null, 2500.00m, 125.00m, desde, desde.Year, desde.Month),
        ];

        return Task.FromResult(notas);
    }
}
