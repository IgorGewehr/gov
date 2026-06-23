using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

/// <summary>Identificador forte do agregado <see cref="Consignataria"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ConsignatariaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ConsignatariaId"/>.</returns>
    public static ConsignatariaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Cadastro mestre de uma ENTIDADE CONSIGNATARIA habilitada (banco/financeira, entidade de classe,
/// seguradora) a receber consignacao em folha do ente (Lei 14.131/2021). So uma consignataria
/// <see cref="SituacaoConsignataria.Ativa"/> pode receber novas averbacoes. Unica por (tenant, CNPJ).
/// Raiz de agregado.
/// </summary>
public sealed class Consignataria : AggregateRoot<ConsignatariaId>, IMustHaveTenant
{
    private Consignataria()
    {
    }

    private Consignataria(
        ConsignatariaId id,
        Guid tenantId,
        Cnpj cnpj,
        string razaoSocial,
        TipoConsignataria tipo)
        : base(id)
    {
        TenantId = tenantId;
        Cnpj = cnpj;
        RazaoSocial = razaoSocial;
        Tipo = tipo;
        Situacao = SituacaoConsignataria.Ativa;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>CNPJ da consignataria (sem mascara; unico por tenant).</summary>
    public Cnpj Cnpj { get; private set; } = default!;

    /// <summary>Razao social/nome empresarial da consignataria.</summary>
    public string RazaoSocial { get; private set; } = default!;

    /// <summary>Natureza da consignataria (apenas classificacao cadastral).</summary>
    public TipoConsignataria Tipo { get; private set; }

    /// <summary>Situacao no cadastro (ativa habilita novas averbacoes).</summary>
    public SituacaoConsignataria Situacao { get; private set; }

    /// <summary>Indica se a consignataria pode receber novas averbacoes.</summary>
    public bool EstaAtiva => Situacao == SituacaoConsignataria.Ativa;

    /// <summary>Cadastra uma consignataria habilitada (nasce Ativa).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="cnpj">CNPJ valido da consignataria.</param>
    /// <param name="razaoSocial">Razao social/nome empresarial.</param>
    /// <param name="tipo">Natureza da consignataria.</param>
    /// <returns>Nova <see cref="Consignataria"/> ativa.</returns>
    /// <exception cref="ArgumentNullException">Se o CNPJ for nulo.</exception>
    /// <exception cref="ArgumentException">Se a razao social for vazia.</exception>
    public static Consignataria Cadastrar(Guid tenantId, Cnpj cnpj, string razaoSocial, TipoConsignataria tipo)
    {
        ArgumentNullException.ThrowIfNull(cnpj);
        ArgumentException.ThrowIfNullOrWhiteSpace(razaoSocial);
        return new Consignataria(ConsignatariaId.New(), tenantId, cnpj, razaoSocial.Trim(), tipo);
    }

    /// <summary>Suspende a consignataria (bloqueia novas averbacoes).</summary>
    /// <exception cref="InvalidOperationException">Se ja estiver suspensa.</exception>
    public void Suspender()
    {
        if (Situacao == SituacaoConsignataria.Suspensa)
        {
            throw new InvalidOperationException("Consignataria ja esta suspensa.");
        }

        Situacao = SituacaoConsignataria.Suspensa;
    }

    /// <summary>Reativa a consignataria (volta a aceitar averbacoes).</summary>
    /// <exception cref="InvalidOperationException">Se ja estiver ativa.</exception>
    public void Reativar()
    {
        if (Situacao == SituacaoConsignataria.Ativa)
        {
            throw new InvalidOperationException("Consignataria ja esta ativa.");
        }

        Situacao = SituacaoConsignataria.Ativa;
    }
}
