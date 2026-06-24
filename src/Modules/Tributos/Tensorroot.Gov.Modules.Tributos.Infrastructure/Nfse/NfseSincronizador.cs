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
    /// <summary>Item da lista de serviços usado quando o ADN não informa o item no documento.</summary>
    private const string ItemListaNaoClassificado = "00.00";

    /// <inheritdoc />
    public async Task<int> SincronizarAsync(IReadOnlyList<string> cnpjsPrestadores, DateOnly desde, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cnpjsPrestadores);

        var importadas = 0;
        var atualizadas = 0;

        foreach (var cnpj in cnpjsPrestadores)
        {
            var documentos = await gateway.ObterNotasEmitidasAsync(cnpj, desde, cancellationToken).ConfigureAwait(false);

            foreach (var documento in documentos)
            {
                if (await notas.ExistePorChaveAsync(documento.ChaveAcesso, cancellationToken).ConfigureAwait(false))
                {
                    // T-W1 — A nota já existe. Antes a dedup descartava QUALQUER reemissão (`continue`),
                    // perdendo eventos de cancelamento/substituição do ADN (mesma chave, nova situação) e
                    // mantendo a nota Normal na base de apuração → ISS apurado sobre nota cancelada. Agora,
                    // se a reemissão sinaliza Cancelada/Substituída, aplicamos o evento ao agregado
                    // (idempotente) para tirá-la da apuração; reemissão Normal segue sendo dedup (no-op).
                    if (documento.Situacao is SituacaoNfse.Cancelada or SituacaoNfse.Substituida)
                    {
                        var existente = await notas.ObterPorChaveAsync(documento.ChaveAcesso, cancellationToken).ConfigureAwait(false);
                        if (existente is not null)
                        {
                            AplicarSituacao(existente, documento.Situacao);
                            atualizadas++;
                        }
                    }

                    continue;
                }

                // Item da lista LC 116 é obrigatório para a apuração do ISS; quando o gateway não o
                // informa (// TODO(validar-oficial): mapear o campo no XSD nacional), usamos um item
                // "não classificado" para não perder a nota — a apuração falhará explicitamente se a
                // tabela municipal não cobrir esse item, em vez de presumir alíquota.
                var itemLista = string.IsNullOrWhiteSpace(documento.ItemListaServico)
                    ? ItemListaNaoClassificado
                    : documento.ItemListaServico;

                var nota = NotaFiscalServico.Importar(
                    tenant.TenantId,
                    documento.ChaveAcesso,
                    documento.PrestadorCnpj,
                    documento.TomadorDocumento,
                    ValorMonetario.De(documento.ValorServico),
                    ValorMonetario.De(documento.ValorIss),
                    documento.DataEmissao,
                    Competencia.De(documento.Ano, documento.Mes),
                    itemLista,
                    documento.IssRetidoNaFonte,
                    documento.MunicipioIncidenciaIbge);

                // Reemissão pode chegar já cancelada/substituída (1ª vez vista por nós): a nota nasce
                // Normal e aplicamos a situação na importação, mantendo o histórico fora da apuração.
                if (documento.Situacao is SituacaoNfse.Cancelada or SituacaoNfse.Substituida)
                {
                    AplicarSituacao(nota, documento.Situacao);
                }

                notas.Adicionar(nota);
                importadas++;
            }
        }

        if (importadas > 0 || atualizadas > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return importadas;
    }

    /// <summary>Aplica ao agregado a situação de cancelamento/substituição ingerida do ADN (idempotente).</summary>
    private static void AplicarSituacao(NotaFiscalServico nota, SituacaoNfse situacao)
    {
        switch (situacao)
        {
            case SituacaoNfse.Cancelada:
                nota.Cancelar();
                break;
            case SituacaoNfse.Substituida:
                nota.Substituir();
                break;
            default:
                // Normal não é um evento de saída da apuração; nada a aplicar.
                break;
        }
    }
}
