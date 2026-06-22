using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Application.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Nfse;

/// <summary>
/// Sincroniza as NFS-e do tenant atual a partir do ADN: baixa via gateway (ACL),
/// deduplica por chave de acesso e persiste como read model fiscal.
/// </summary>
public sealed class NfseSincronizador(
    INfseNacionalGateway gateway,
    INotaFiscalServicoRepository notas,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : INfseSincronizador
{
    /// <inheritdoc />
    public async Task<int> SincronizarAsync(IReadOnlyList<string> cnpjsPrestadores, DateOnly desde, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cnpjsPrestadores);

        var importadas = 0;

        foreach (var cnpj in cnpjsPrestadores)
        {
            var documentos = await gateway.ObterNotasEmitidasAsync(cnpj, desde, cancellationToken).ConfigureAwait(false);

            foreach (var documento in documentos)
            {
                if (await notas.ExistePorChaveAsync(documento.ChaveAcesso, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                var nota = NotaFiscalServico.Importar(
                    tenant.TenantId,
                    documento.ChaveAcesso,
                    documento.PrestadorCnpj,
                    documento.TomadorDocumento,
                    ValorMonetario.De(documento.ValorServico),
                    ValorMonetario.De(documento.ValorIss),
                    documento.DataEmissao,
                    Competencia.De(documento.Ano, documento.Mes));

                notas.Adicionar(nota);
                importadas++;
            }
        }

        if (importadas > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return importadas;
    }
}
