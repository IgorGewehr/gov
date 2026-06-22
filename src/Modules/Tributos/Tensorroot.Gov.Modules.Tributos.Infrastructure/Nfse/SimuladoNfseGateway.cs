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

        // Itens LC 116 e indicador de retenção são ilustrativos (dev). // TODO(validar-oficial):
        // valores reais vêm do XSD da NFS-e nacional ingerido do ADN.
        IReadOnlyList<NfseDocumento> notas =
        [
            new($"NFSE-{cnpjPrestador}-0001", cnpjPrestador, null, 1000.00m, 50.00m, desde, desde.Year, desde.Month, ItemListaServico: "1.07"),
            new($"NFSE-{cnpjPrestador}-0002", cnpjPrestador, null, 2500.00m, 125.00m, desde, desde.Year, desde.Month, ItemListaServico: "7.02", IssRetidoNaFonte: true),
        ];

        return Task.FromResult(notas);
    }
}
