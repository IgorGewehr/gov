using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Sim;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Events;

/// <summary>Requerimento de registro no S.I.M. protocolado (entra em análise).</summary>
/// <param name="TituloRegistroSimId">Identificador do título.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ResponsavelId">Contribuinte responsável legal.</param>
public sealed record TituloRegistroSimRequerido(TituloRegistroSimId TituloRegistroSimId, Guid TenantId, ContribuinteId ResponsavelId) : IDomainEvent;

/// <summary>Título de registro no S.I.M. concedido (número do S.I.M. atribuído).</summary>
/// <param name="TituloRegistroSimId">Identificador do título.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ResponsavelId">Contribuinte responsável legal.</param>
/// <param name="NumeroSim">Número do S.I.M. atribuído.</param>
public sealed record TituloRegistroSimConcedido(TituloRegistroSimId TituloRegistroSimId, Guid TenantId, ContribuinteId ResponsavelId, string NumeroSim) : IDomainEvent;

/// <summary>Título de registro no S.I.M. suspenso por irregularidade sanitária.</summary>
/// <param name="TituloRegistroSimId">Identificador do título.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Motivo">Motivo da suspensão.</param>
public sealed record TituloRegistroSimSuspenso(TituloRegistroSimId TituloRegistroSimId, Guid TenantId, string Motivo) : IDomainEvent;

/// <summary>Título de registro no S.I.M. cassado/cancelado.</summary>
/// <param name="TituloRegistroSimId">Identificador do título.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="Motivo">Motivo da cassação.</param>
public sealed record TituloRegistroSimCassado(TituloRegistroSimId TituloRegistroSimId, Guid TenantId, string Motivo) : IDomainEvent;

/// <summary>Título de registro no S.I.M. renovado por novo período.</summary>
/// <param name="TituloRegistroSimId">Identificador do título.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="NovoFimVigencia">Novo fim de vigência.</param>
public sealed record TituloRegistroSimRenovado(TituloRegistroSimId TituloRegistroSimId, Guid TenantId, DateOnly NovoFimVigencia) : IDomainEvent;
