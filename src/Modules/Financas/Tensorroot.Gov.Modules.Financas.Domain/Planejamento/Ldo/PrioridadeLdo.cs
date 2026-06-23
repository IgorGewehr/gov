using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;

/// <summary>
/// Prioridade/meta da LDO: seleciona uma ação do PPA como prioritária para o exercício
/// (CF 165 §2º — a LDO compreende as metas e prioridades extraídas do PPA).
/// Entidade-filha de <see cref="LeiDiretrizes"/>.
/// </summary>
public sealed class PrioridadeLdo : Entity<PrioridadeLdoId>
{
    private PrioridadeLdo()
    {
    }

    private PrioridadeLdo(PrioridadeLdoId id, LdoId ldoId, AcaoPpaId acaoPpaId, int ordem, string justificativa)
        : base(id)
    {
        LdoId = ldoId;
        AcaoPpaId = acaoPpaId;
        Ordem = ordem;
        Justificativa = justificativa;
    }

    /// <summary>LDO à qual a prioridade pertence.</summary>
    public LdoId LdoId { get; private set; }

    /// <summary>Ação do PPA priorizada.</summary>
    public AcaoPpaId AcaoPpaId { get; private set; }

    /// <summary>Ordem de prioridade.</summary>
    public int Ordem { get; private set; }

    /// <summary>Justificativa da priorização.</summary>
    public string Justificativa { get; private set; } = default!;

    /// <summary>Cria uma prioridade válida.</summary>
    /// <param name="ldoId">LDO dona da prioridade.</param>
    /// <param name="acaoPpaId">Ação do PPA priorizada.</param>
    /// <param name="ordem">Ordem de prioridade (positiva).</param>
    /// <param name="justificativa">Justificativa.</param>
    /// <returns>Nova <see cref="PrioridadeLdo"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a ordem não for positiva.</exception>
    internal static PrioridadeLdo Criar(LdoId ldoId, AcaoPpaId acaoPpaId, int ordem, string justificativa)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justificativa);
        ArgumentOutOfRangeException.ThrowIfLessThan(ordem, 1);
        return new PrioridadeLdo(PrioridadeLdoId.New(), ldoId, acaoPpaId, ordem, justificativa.Trim());
    }
}
