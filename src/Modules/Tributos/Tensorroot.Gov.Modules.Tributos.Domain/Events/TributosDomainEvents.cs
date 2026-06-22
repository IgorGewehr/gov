using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Events;

/// <summary>Contribuinte cadastrado.</summary>
/// <param name="ContribuinteId">Identificador do contribuinte.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
public sealed record ContribuinteCadastrado(ContribuinteId ContribuinteId, Guid TenantId) : IDomainEvent;

/// <summary>Crédito tributário lançado (constituído).</summary>
/// <param name="LancamentoId">Identificador do lançamento.</param>
/// <param name="ContribuinteId">Contribuinte devedor.</param>
/// <param name="ValorPrincipal">Valor principal lançado.</param>
public sealed record CreditoTributarioLancado(LancamentoId LancamentoId, ContribuinteId ContribuinteId, decimal ValorPrincipal) : IDomainEvent;

/// <summary>Pagamento de um lançamento registrado.</summary>
/// <param name="LancamentoId">Identificador do lançamento pago.</param>
public sealed record PagamentoRegistrado(LancamentoId LancamentoId) : IDomainEvent;

/// <summary>Lançamento inscrito em Dívida Ativa.</summary>
/// <param name="LancamentoId">Lançamento de origem.</param>
/// <param name="ContribuinteId">Contribuinte devedor.</param>
public sealed record LancamentoInscritoEmDividaAtiva(LancamentoId LancamentoId, ContribuinteId ContribuinteId) : IDomainEvent;

/// <summary>Dívida Ativa inscrita.</summary>
/// <param name="DividaAtivaId">Identificador da dívida ativa.</param>
/// <param name="ContribuinteId">Contribuinte devedor.</param>
/// <param name="LancamentoId">Lançamento de origem.</param>
public sealed record DividaAtivaInscrita(DividaAtivaId DividaAtivaId, ContribuinteId ContribuinteId, LancamentoId LancamentoId) : IDomainEvent;

/// <summary>Certidão de Dívida Ativa (CDA) emitida.</summary>
/// <param name="DividaAtivaId">Identificador da dívida ativa.</param>
/// <param name="NumeroCda">Número da CDA.</param>
public sealed record CdaEmitida(DividaAtivaId DividaAtivaId, string NumeroCda) : IDomainEvent;

/// <summary>Parcelamento (REFIS) firmado — suspende a exigibilidade e interrompe a prescrição.</summary>
/// <param name="DividaAtivaId">Identificador da dívida ativa.</param>
public sealed record ParcelamentoFirmado(DividaAtivaId DividaAtivaId) : IDomainEvent;

/// <summary>Dívida Ativa quitada.</summary>
/// <param name="DividaAtivaId">Identificador da dívida ativa.</param>
public sealed record DividaQuitada(DividaAtivaId DividaAtivaId) : IDomainEvent;
