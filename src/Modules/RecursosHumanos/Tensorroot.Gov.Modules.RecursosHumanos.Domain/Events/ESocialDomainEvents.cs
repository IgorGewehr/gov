using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Evento eSocial gerado a partir do dominio (estado inicial <c>Gerado</c>).</summary>
/// <param name="EventoESocialId">Identificador do evento eSocial.</param>
/// <param name="Tipo">Tipo do evento (S-1000, S-1200, ...).</param>
/// <param name="IdEvento">Atributo Id do evento no XML.</param>
public sealed record EventoESocialGerado(EventoESocialId EventoESocialId, TipoEventoESocial Tipo, string IdEvento) : IDomainEvent;

/// <summary>Evento eSocial assinado em XML-DSig com o A1 do ente (estado <c>Assinado</c>).</summary>
/// <param name="EventoESocialId">Identificador do evento eSocial.</param>
/// <param name="Tipo">Tipo do evento.</param>
public sealed record EventoESocialAssinado(EventoESocialId EventoESocialId, TipoEventoESocial Tipo) : IDomainEvent;

/// <summary>Evento eSocial transmitido (lote enviado; protocolo recebido — estado <c>Transmitido</c>).</summary>
/// <param name="EventoESocialId">Identificador do evento eSocial.</param>
/// <param name="Tipo">Tipo do evento.</param>
/// <param name="ProtocoloLote">Protocolo de envio do lote.</param>
public sealed record EventoESocialTransmitido(EventoESocialId EventoESocialId, TipoEventoESocial Tipo, string ProtocoloLote) : IDomainEvent;

/// <summary>Evento eSocial processado e aceito (recibo recebido — estado terminal <c>Processado</c>).</summary>
/// <param name="EventoESocialId">Identificador do evento eSocial.</param>
/// <param name="Tipo">Tipo do evento.</param>
/// <param name="NumeroRecibo">Recibo (nrRecibo) por evento.</param>
public sealed record EventoESocialProcessado(EventoESocialId EventoESocialId, TipoEventoESocial Tipo, string NumeroRecibo) : IDomainEvent;

/// <summary>Evento eSocial rejeitado (localmente por XSD, ou pelo eSocial no processamento).</summary>
/// <param name="EventoESocialId">Identificador do evento eSocial.</param>
/// <param name="Tipo">Tipo do evento.</param>
/// <param name="Codigo">Codigo do erro/ocorrencia (quando houver).</param>
/// <param name="Descricao">Descricao do erro.</param>
/// <param name="Localizado">Verdadeiro se a rejeicao foi local (XSD), antes de gastar cota.</param>
public sealed record EventoESocialRejeitado(EventoESocialId EventoESocialId, TipoEventoESocial Tipo, string? Codigo, string Descricao, bool Localizado) : IDomainEvent;
