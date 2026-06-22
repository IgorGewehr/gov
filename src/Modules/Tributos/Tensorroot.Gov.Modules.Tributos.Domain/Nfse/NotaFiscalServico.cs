using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Nfse;

/// <summary>Identificador forte do read model <see cref="NotaFiscalServico"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct NotaFiscalServicoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="NotaFiscalServicoId"/>.</returns>
    public static NotaFiscalServicoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Situação (vigência fiscal) da NFS-e segundo os eventos ingeridos do ADN. Notas canceladas ou
/// substituídas SAEM da base de apuração do ISS, mas o histórico é mantido (auditoria, CLAUDE.md §6).
/// // TODO(validar-oficial): códigos exatos de eventos no manual de eventos do leiaute nacional (M6-DESIGN §2.1).
/// </summary>
public enum SituacaoNfse
{
    /// <summary>Normal (vigente, entra na apuração).</summary>
    Normal = 1,

    /// <summary>Cancelada por evento (sai da apuração).</summary>
    Cancelada = 2,

    /// <summary>Substituída por outra NFS-e (sai da apuração; a substituta a referencia).</summary>
    Substituida = 3,
}

/// <summary>NFS-e do Ambiente Nacional importada para o painel fiscal.</summary>
/// <param name="NotaFiscalServicoId">Identificador da nota.</param>
/// <param name="ChaveAcesso">Chave de acesso da NFS-e.</param>
public sealed record NotaFiscalServicoImportada(NotaFiscalServicoId NotaFiscalServicoId, string ChaveAcesso) : IDomainEvent;

/// <summary>
/// NFS-e (Nota Fiscal de Serviço eletrônica) sincronizada do Ambiente de Dados Nacional (ADN).
/// Read model para fiscalização e apuração do ISS — NÃO emitimos nem assinamos a nota (ADR-0003);
/// apenas a consumimos. Carrega os campos necessários à apuração (item LC 116, retenção, município
/// de incidência) e a situação fiscal derivada de eventos. Ver M6-DESIGN §2.
/// </summary>
public sealed class NotaFiscalServico : AggregateRoot<NotaFiscalServicoId>, IMustHaveTenant
{
    private NotaFiscalServico()
    {
    }

    private NotaFiscalServico(
        NotaFiscalServicoId id,
        Guid tenantId,
        string chaveAcesso,
        string prestadorCnpj,
        string? tomadorDocumento,
        ValorMonetario valorServico,
        ValorMonetario valorIss,
        DateOnly dataEmissao,
        Competencia competencia,
        string itemListaServico,
        bool issRetidoNaFonte,
        string? municipioIncidenciaIbge)
        : base(id)
    {
        TenantId = tenantId;
        ChaveAcesso = chaveAcesso;
        PrestadorCnpj = prestadorCnpj;
        TomadorDocumento = tomadorDocumento;
        ValorServico = valorServico;
        ValorIss = valorIss;
        DataEmissao = dataEmissao;
        Competencia = competencia;
        ItemListaServico = itemListaServico;
        IssRetidoNaFonte = issRetidoNaFonte;
        MunicipioIncidenciaIbge = municipioIncidenciaIbge;
        Situacao = SituacaoNfse.Normal;
        RaiseDomainEvent(new NotaFiscalServicoImportada(id, chaveAcesso));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave de acesso (identificador nacional único da NFS-e).</summary>
    public string ChaveAcesso { get; private set; } = default!;

    /// <summary>CNPJ do prestador (emissor).</summary>
    public string PrestadorCnpj { get; private set; } = default!;

    /// <summary>Documento do tomador, quando informado.</summary>
    public string? TomadorDocumento { get; private set; }

    /// <summary>Valor do serviço.</summary>
    public ValorMonetario ValorServico { get; private set; } = default!;

    /// <summary>Valor do ISS destacado.</summary>
    public ValorMonetario ValorIss { get; private set; } = default!;

    /// <summary>Data de emissão.</summary>
    public DateOnly DataEmissao { get; private set; }

    /// <summary>Competência fiscal.</summary>
    public Competencia Competencia { get; private set; } = default!;

    /// <summary>
    /// Item da lista de serviços da LC 116/2003 (ex.: "7.02", "17.01"). Chave de busca da alíquota,
    /// do mapa de retenção e da lista de substitutos — todos parametrizados por lei municipal.
    /// // TODO(validar-oficial): campo exato do item da lista no XSD da NFS-e nacional (M6-DESIGN §2.1).
    /// </summary>
    public string ItemListaServico { get; private set; } = default!;

    /// <summary>Indicador (do XML) de que o ISS foi retido na fonte pelo tomador (LC 116 art. 6º).</summary>
    public bool IssRetidoNaFonte { get; private set; }

    /// <summary>Código IBGE do município de incidência do ISS (local da prestação — LC 116 art. 3º), quando informado.</summary>
    public string? MunicipioIncidenciaIbge { get; private set; }

    /// <summary>Situação fiscal vigente (normal/cancelada/substituída) conforme eventos ingeridos.</summary>
    public SituacaoNfse Situacao { get; private set; }

    /// <summary>Indica se a nota está vigente para fins de apuração (situação normal).</summary>
    public bool VigenteParaApuracao => Situacao == SituacaoNfse.Normal;

    /// <summary>Importa (registra) uma NFS-e baixada do ADN.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="chaveAcesso">Chave de acesso.</param>
    /// <param name="prestadorCnpj">CNPJ do prestador.</param>
    /// <param name="tomadorDocumento">Documento do tomador (opcional).</param>
    /// <param name="valorServico">Valor do serviço.</param>
    /// <param name="valorIss">Valor do ISS.</param>
    /// <param name="dataEmissao">Data de emissão.</param>
    /// <param name="competencia">Competência fiscal.</param>
    /// <param name="itemListaServico">Item da lista de serviços LC 116/2003.</param>
    /// <param name="issRetidoNaFonte">Indicador de retenção do ISS na fonte.</param>
    /// <param name="municipioIncidenciaIbge">Código IBGE do município de incidência (opcional).</param>
    /// <returns>Nova <see cref="NotaFiscalServico"/>.</returns>
    public static NotaFiscalServico Importar(
        Guid tenantId,
        string chaveAcesso,
        string prestadorCnpj,
        string? tomadorDocumento,
        ValorMonetario valorServico,
        ValorMonetario valorIss,
        DateOnly dataEmissao,
        Competencia competencia,
        string itemListaServico,
        bool issRetidoNaFonte = false,
        string? municipioIncidenciaIbge = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaveAcesso);
        ArgumentException.ThrowIfNullOrWhiteSpace(prestadorCnpj);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemListaServico);
        ArgumentNullException.ThrowIfNull(valorServico);
        ArgumentNullException.ThrowIfNull(valorIss);
        ArgumentNullException.ThrowIfNull(competencia);

        return new NotaFiscalServico(
            NotaFiscalServicoId.New(),
            tenantId,
            chaveAcesso,
            prestadorCnpj,
            tomadorDocumento,
            valorServico,
            valorIss,
            dataEmissao,
            competencia,
            itemListaServico.Trim(),
            issRetidoNaFonte,
            string.IsNullOrWhiteSpace(municipioIncidenciaIbge) ? null : municipioIncidenciaIbge.Trim());
    }

    /// <summary>
    /// Aplica um evento de cancelamento da NFS-e (ingerido do ADN): a nota sai da base de apuração,
    /// mantendo histórico. Idempotente — reaplicar não altera o estado.
    /// </summary>
    public void Cancelar()
    {
        if (Situacao == SituacaoNfse.Cancelada)
        {
            return;
        }

        Situacao = SituacaoNfse.Cancelada;
    }

    /// <summary>
    /// Aplica um evento de substituição da NFS-e (ingerido do ADN): a nota sai da base de apuração;
    /// a NFS-e substituta a referencia pela chave. Idempotente.
    /// </summary>
    public void Substituir()
    {
        if (Situacao == SituacaoNfse.Substituida)
        {
            return;
        }

        Situacao = SituacaoNfse.Substituida;
    }
}
