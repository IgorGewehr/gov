using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Itbi;

/// <summary>Identificador forte do agregado <see cref="AliquotaItbi"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AliquotaItbiId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AliquotaItbiId"/>.</returns>
    public static AliquotaItbiId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Alíquota do ITBI vigente por exercício (LEI MUNICIPAL — não há teto federal). Distingue a alíquota
/// geral da alíquota reduzida sobre a parcela financiada pelo SFH. Nenhum número é hardcoded — tudo
/// vem da lei do município. Ver M6-DESIGN §3.1.
/// </summary>
public sealed class AliquotaItbi : AggregateRoot<AliquotaItbiId>, IMustHaveTenant
{
    private AliquotaItbi()
    {
    }

    private AliquotaItbi(
        AliquotaItbiId id,
        Guid tenantId,
        int exercicio,
        decimal aliquotaGeralPercentual,
        decimal aliquotaSfhFinanciadaPercentual,
        string fundamentoLegal)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        AliquotaGeralPercentual = aliquotaGeralPercentual;
        AliquotaSfhFinanciadaPercentual = aliquotaSfhFinanciadaPercentual;
        FundamentoLegal = fundamentoLegal;
        Vigente = false;
        RaiseDomainEvent(new AliquotaItbiCriada(id, tenantId, exercicio));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício fiscal de vigência.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Alíquota geral em % (ex.: 2.0 = 2%).</summary>
    public decimal AliquotaGeralPercentual { get; private set; }

    /// <summary>
    /// Alíquota em % sobre a parcela financiada pelo SFH (usualmente reduzida). Igual à geral quando
    /// o município não distingue. // TODO(validar-oficial): existência/valor da redução SFH no CTM
    /// de Maximiliano de Almeida/RS.
    /// </summary>
    public decimal AliquotaSfhFinanciadaPercentual { get; private set; }

    /// <summary>
    /// Fundamento legal (CTM/lei municipal do ITBI).
    /// // TODO(validar-oficial): preencher com a lei de Maximiliano de Almeida/RS.
    /// </summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Indica se a alíquota está vigente (publicada e imutável).</summary>
    public bool Vigente { get; private set; }

    /// <summary>Cria a configuração de alíquota do ITBI de um exercício (ainda não vigente).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício fiscal (ano ≥ 1900).</param>
    /// <param name="aliquotaGeralPercentual">Alíquota geral em %.</param>
    /// <param name="aliquotaSfhFinanciadaPercentual">Alíquota da parcela financiada pelo SFH em % (use a geral se não houver redução).</param>
    /// <param name="fundamentoLegal">Lei municipal do ITBI.</param>
    /// <returns>Nova <see cref="AliquotaItbi"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se ano ou alíquotas forem inválidos.</exception>
    public static AliquotaItbi Criar(
        Guid tenantId,
        int exercicio,
        decimal aliquotaGeralPercentual,
        decimal aliquotaSfhFinanciadaPercentual,
        string fundamentoLegal)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 1900);
        if (aliquotaGeralPercentual is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(aliquotaGeralPercentual), aliquotaGeralPercentual, "A alíquota do ITBI deve estar entre 0 e 100.");
        }

        if (aliquotaSfhFinanciadaPercentual is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(aliquotaSfhFinanciadaPercentual), aliquotaSfhFinanciadaPercentual, "A alíquota SFH do ITBI deve estar entre 0 e 100.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        return new AliquotaItbi(AliquotaItbiId.New(), tenantId, exercicio, aliquotaGeralPercentual, aliquotaSfhFinanciadaPercentual, fundamentoLegal.Trim());
    }

    /// <summary>Publica a configuração (torna vigente e imutável).</summary>
    public void Publicar()
    {
        Vigente = true;
        RaiseDomainEvent(new AliquotaItbiPublicada(Id, TenantId, Exercicio));
    }
}
