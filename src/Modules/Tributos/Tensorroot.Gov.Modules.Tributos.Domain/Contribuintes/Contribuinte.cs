using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;

/// <summary>Identificador forte do agregado <see cref="Contribuinte"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ContribuinteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ContribuinteId"/>.</returns>
    public static ContribuinteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Natureza jurídica do contribuinte.</summary>
public enum TipoPessoa
{
    /// <summary>Pessoa física (identificada por CPF).</summary>
    Fisica = 1,

    /// <summary>Pessoa jurídica (identificada por CNPJ).</summary>
    Juridica = 2,
}

/// <summary>Contribuinte municipal (pessoa física ou jurídica) sujeito a tributos.</summary>
public sealed class Contribuinte : AggregateRoot<ContribuinteId>, IMustHaveTenant
{
    private Contribuinte()
    {
    }

    private Contribuinte(
        ContribuinteId id,
        Guid tenantId,
        TipoPessoa tipoPessoa,
        string documento,
        string nome,
        string? inscricaoMunicipal)
        : base(id)
    {
        TenantId = tenantId;
        TipoPessoa = tipoPessoa;
        Documento = documento;
        Nome = nome;
        InscricaoMunicipal = inscricaoMunicipal;
        RaiseDomainEvent(new ContribuinteCadastrado(id, tenantId));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Natureza jurídica.</summary>
    public TipoPessoa TipoPessoa { get; private set; }

    /// <summary>Documento (CPF ou CNPJ) sem máscara.</summary>
    public string Documento { get; private set; } = default!;

    /// <summary>Nome ou razão social.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Inscrição municipal (mobiliária), quando houver.</summary>
    public string? InscricaoMunicipal { get; private set; }

    /// <summary>Cadastra um contribuinte pessoa física.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="cpf">CPF válido.</param>
    /// <param name="nome">Nome completo.</param>
    /// <param name="inscricaoMunicipal">Inscrição municipal opcional.</param>
    /// <returns>Novo <see cref="Contribuinte"/>.</returns>
    public static Contribuinte PessoaFisica(Guid tenantId, Cpf cpf, string nome, string? inscricaoMunicipal = null)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        return new Contribuinte(ContribuinteId.New(), tenantId, TipoPessoa.Fisica, cpf.Digitos, nome, inscricaoMunicipal);
    }

    /// <summary>Cadastra um contribuinte pessoa jurídica.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="cnpj">CNPJ válido.</param>
    /// <param name="razaoSocial">Razão social.</param>
    /// <param name="inscricaoMunicipal">Inscrição municipal opcional.</param>
    /// <returns>Novo <see cref="Contribuinte"/>.</returns>
    public static Contribuinte PessoaJuridica(Guid tenantId, Cnpj cnpj, string razaoSocial, string? inscricaoMunicipal = null)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        ArgumentException.ThrowIfNullOrWhiteSpace(razaoSocial);
        return new Contribuinte(ContribuinteId.New(), tenantId, TipoPessoa.Juridica, cnpj.Digitos, razaoSocial, inscricaoMunicipal);
    }
}
