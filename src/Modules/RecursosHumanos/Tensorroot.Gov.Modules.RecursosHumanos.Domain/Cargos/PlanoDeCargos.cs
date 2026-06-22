using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

/// <summary>Identificador forte da entidade <see cref="PlanoDeCargos"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PlanoDeCargosId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PlanoDeCargosId"/>.</returns>
    public static PlanoDeCargosId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Plano de cargos: entidade que agrupa cargos por carreira/estrutura de pessoal do ente publico.
/// Associada a raiz <see cref="Cargo"/> em relacao 1:N.
/// </summary>
public sealed class PlanoDeCargos : Entity<PlanoDeCargosId>, IMustHaveTenant
{
    private PlanoDeCargos()
    {
    }

    private PlanoDeCargos(PlanoDeCargosId id, Guid tenantId, string denominacaoCarreira)
        : base(id)
    {
        TenantId = tenantId;
        DenominacaoCarreira = denominacaoCarreira;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Denominacao da carreira/estrutura agrupada por este plano.</summary>
    public string DenominacaoCarreira { get; private set; } = default!;

    /// <summary>Cria um plano de cargos.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="denominacaoCarreira">Denominacao da carreira.</param>
    /// <returns>Novo <see cref="PlanoDeCargos"/>.</returns>
    /// <exception cref="ArgumentException">Se a denominacao da carreira for vazia.</exception>
    public static PlanoDeCargos Criar(Guid tenantId, string denominacaoCarreira)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(denominacaoCarreira);
        return new PlanoDeCargos(PlanoDeCargosId.New(), tenantId, denominacaoCarreira.Trim());
    }
}
