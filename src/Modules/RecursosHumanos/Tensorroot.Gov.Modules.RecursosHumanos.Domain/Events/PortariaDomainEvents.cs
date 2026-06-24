using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Portaria de pessoal emitida (situacao inicial <c>Emitida</c>).</summary>
/// <param name="PortariaId">Identificador da portaria.</param>
/// <param name="Numero">Numeracao oficial atribuida (sequencial/exercicio).</param>
/// <param name="Tipo">Natureza do ato de pessoal.</param>
/// <param name="ServidorId">Servidor vinculado ao ato, quando aplicavel.</param>
public sealed record PortariaEmitida(PortariaId PortariaId, NumeroPortaria Numero, TipoPortaria Tipo, ServidorId? ServidorId) : IDomainEvent;

/// <summary>Portaria revogada/tornada sem efeito (situacao <c>Revogada</c>).</summary>
/// <param name="PortariaId">Identificador da portaria.</param>
/// <param name="Motivo">Fundamento da revogacao.</param>
public sealed record PortariaRevogada(PortariaId PortariaId, string Motivo) : IDomainEvent;
