using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Domicilio;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Events;

/// <summary>Contribuinte aderiu ao Domicílio Eletrônico do Contribuinte (DEC).</summary>
/// <param name="DomicilioId">Identificador do domicílio.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ContribuinteId">Contribuinte titular.</param>
public sealed record DomicilioEletronicoAderido(DomicilioEletronicoContribuinteId DomicilioId, Guid TenantId, ContribuinteId ContribuinteId) : IDomainEvent;

/// <summary>Adesão ao Domicílio Eletrônico cancelada (desativado).</summary>
/// <param name="DomicilioId">Identificador do domicílio.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ContribuinteId">Contribuinte titular.</param>
public sealed record DomicilioEletronicoCancelado(DomicilioEletronicoContribuinteId DomicilioId, Guid TenantId, ContribuinteId ContribuinteId) : IDomainEvent;

/// <summary>Mensagem fiscal disponibilizada no Domicílio Eletrônico (efeito de intimação a partir da ciência).</summary>
/// <param name="DomicilioId">Identificador do domicílio.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ContribuinteId">Contribuinte titular.</param>
/// <param name="MensagemId">Identificador da mensagem.</param>
/// <param name="Tipo">Tipo da comunicação.</param>
public sealed record MensagemFiscalDisponibilizada(
    DomicilioEletronicoContribuinteId DomicilioId,
    Guid TenantId,
    ContribuinteId ContribuinteId,
    MensagemFiscalId MensagemId,
    TipoMensagemFiscal Tipo) : IDomainEvent;

/// <summary>Ciência de mensagem fiscal registrada (expressa pela consulta ou tácita pelo decurso de prazo).</summary>
/// <param name="DomicilioId">Identificador do domicílio.</param>
/// <param name="TenantId">Tenant dono do registro.</param>
/// <param name="ContribuinteId">Contribuinte titular.</param>
/// <param name="MensagemId">Identificador da mensagem.</param>
/// <param name="DataCiencia">Data da ciência (data do fato).</param>
/// <param name="Tacita"><c>true</c> se a ciência foi tácita (decurso de prazo).</param>
public sealed record CienciaMensagemFiscalRegistrada(
    DomicilioEletronicoContribuinteId DomicilioId,
    Guid TenantId,
    ContribuinteId ContribuinteId,
    MensagemFiscalId MensagemId,
    DateOnly DataCiencia,
    bool Tacita) : IDomainEvent;
