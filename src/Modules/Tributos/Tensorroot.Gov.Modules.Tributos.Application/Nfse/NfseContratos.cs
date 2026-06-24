using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;

namespace Tensorroot.Gov.Modules.Tributos.Application.Nfse;

/// <summary>Documento NFS-e bruto retornado pelo Ambiente de Dados Nacional (ADN).</summary>
/// <param name="ChaveAcesso">Chave de acesso (identificador nacional).</param>
/// <param name="PrestadorCnpj">CNPJ do prestador.</param>
/// <param name="TomadorDocumento">Documento do tomador (opcional).</param>
/// <param name="ValorServico">Valor do serviço.</param>
/// <param name="ValorIss">Valor do ISS.</param>
/// <param name="DataEmissao">Data de emissão.</param>
/// <param name="Ano">Ano da competência.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="ItemListaServico">Item da lista de serviços LC 116/2003 (chave da alíquota/retenção). // TODO(validar-oficial): campo exato no XSD da NFS-e nacional.</param>
/// <param name="IssRetidoNaFonte">Indicador (do XML) de retenção do ISS na fonte pelo tomador.</param>
/// <param name="MunicipioIncidenciaIbge">Código IBGE do município de incidência do ISS (opcional).</param>
/// <param name="Situacao">
/// Situação fiscal do documento ingerido (Normal/Cancelada/Substituída). Quando o ADN reemite a MESMA
/// chave com situação Cancelada/Substituída (evento de cancelamento/substituição), a sincronização tira
/// a nota da apuração em vez de descartar o evento (T-W1). // TODO(validar-oficial): mapear os códigos
/// exatos do manual de eventos do leiaute nacional para esta enumeração.
/// </param>
public sealed record NfseDocumento(
    string ChaveAcesso,
    string PrestadorCnpj,
    string? TomadorDocumento,
    decimal ValorServico,
    decimal ValorIss,
    DateOnly DataEmissao,
    int Ano,
    int Mes,
    string ItemListaServico = "",
    bool IssRetidoNaFonte = false,
    string? MunicipioIncidenciaIbge = null,
    SituacaoNfse Situacao = SituacaoNfse.Normal);

/// <summary>Gateway (Anti-Corruption Layer) de acesso ao Ambiente de Dados Nacional da NFS-e.</summary>
public interface INfseNacionalGateway
{
    /// <summary>Obtém as NFS-e emitidas por um prestador desde uma data.</summary>
    /// <param name="cnpjPrestador">CNPJ do prestador.</param>
    /// <param name="desde">Data inicial.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Documentos NFS-e encontrados.</returns>
    Task<IReadOnlyList<NfseDocumento>> ObterNotasEmitidasAsync(string cnpjPrestador, DateOnly desde, CancellationToken cancellationToken);
}

/// <summary>Repositório do read model <see cref="NotaFiscalServico"/>.</summary>
public interface INotaFiscalServicoRepository
{
    /// <summary>Indica se já existe nota com a chave de acesso (no tenant atual).</summary>
    /// <param name="chaveAcesso">Chave de acesso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já existir.</returns>
    Task<bool> ExistePorChaveAsync(string chaveAcesso, CancellationToken cancellationToken);

    /// <summary>Obtém a nota persistida pela chave de acesso (no tenant atual), ou <c>null</c> se não existir.</summary>
    /// <param name="chaveAcesso">Chave de acesso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A nota rastreada para mutação (cancelar/substituir), ou <c>null</c>.</returns>
    Task<NotaFiscalServico?> ObterPorChaveAsync(string chaveAcesso, CancellationToken cancellationToken);

    /// <summary>Marca uma nota para inserção.</summary>
    /// <param name="nota">Nota a adicionar.</param>
    void Adicionar(NotaFiscalServico nota);
}

/// <summary>Serviço de sincronização das NFS-e do tenant atual a partir do ADN.</summary>
public interface INfseSincronizador
{
    /// <summary>Baixa, deduplica (por chave de acesso) e persiste as NFS-e dos CNPJs informados.</summary>
    /// <param name="cnpjsPrestadores">CNPJs prestadores do tenant.</param>
    /// <param name="desde">Data inicial da janela de sincronização.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de notas novas importadas.</returns>
    Task<int> SincronizarAsync(IReadOnlyList<string> cnpjsPrestadores, DateOnly desde, CancellationToken cancellationToken);
}
