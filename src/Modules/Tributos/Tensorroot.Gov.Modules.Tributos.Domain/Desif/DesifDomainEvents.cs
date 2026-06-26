using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Desif;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Events;

/// <summary>
/// Declaração DES-IF (Módulo 2 — Apuração Mensal do ISSQN) entregue pela instituição financeira:
/// constitui o crédito do ISSQN a recolher por homologação (CTN art. 150). Dispara o lançamento a jusante.
/// </summary>
/// <param name="DeclaracaoDesifId">Identificador da declaração.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ContribuinteId">Contribuinte declarante (instituição financeira).</param>
/// <param name="IssqnARecolher">ISSQN mensal a recolher após deduções (R$).</param>
public sealed record DeclaracaoDesifEntregue(
    DeclaracaoDesifId DeclaracaoDesifId,
    Guid TenantId,
    ContribuinteId ContribuinteId,
    decimal IssqnARecolher) : IDomainEvent;
