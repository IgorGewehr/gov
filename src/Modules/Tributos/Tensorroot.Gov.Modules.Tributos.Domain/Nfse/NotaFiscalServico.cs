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

/// <summary>NFS-e do Ambiente Nacional importada para o painel fiscal.</summary>
/// <param name="NotaFiscalServicoId">Identificador da nota.</param>
/// <param name="ChaveAcesso">Chave de acesso da NFS-e.</param>
public sealed record NotaFiscalServicoImportada(NotaFiscalServicoId NotaFiscalServicoId, string ChaveAcesso) : IDomainEvent;

/// <summary>
/// NFS-e (Nota Fiscal de Serviço eletrônica) sincronizada do Ambiente de Dados Nacional (ADN).
/// Read model para fiscalização do ISS — NÃO emitimos nem assinamos a nota (ADR-0003); apenas a consumimos.
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
        Competencia competencia)
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

    /// <summary>Importa (registra) uma NFS-e baixada do ADN.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="chaveAcesso">Chave de acesso.</param>
    /// <param name="prestadorCnpj">CNPJ do prestador.</param>
    /// <param name="tomadorDocumento">Documento do tomador (opcional).</param>
    /// <param name="valorServico">Valor do serviço.</param>
    /// <param name="valorIss">Valor do ISS.</param>
    /// <param name="dataEmissao">Data de emissão.</param>
    /// <param name="competencia">Competência fiscal.</param>
    /// <returns>Nova <see cref="NotaFiscalServico"/>.</returns>
    public static NotaFiscalServico Importar(
        Guid tenantId,
        string chaveAcesso,
        string prestadorCnpj,
        string? tomadorDocumento,
        ValorMonetario valorServico,
        ValorMonetario valorIss,
        DateOnly dataEmissao,
        Competencia competencia)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chaveAcesso);
        ArgumentException.ThrowIfNullOrWhiteSpace(prestadorCnpj);
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
            competencia);
    }
}
