using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Events;

/// <summary>Pedido e-SIC protocolado (LAI). NAO carrega PII do solicitante (trilha minimizada).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Protocolo">Protocolo gerado (AAAA/NNNNNN).</param>
/// <param name="PrazoResposta">Prazo legal de resposta (20 dias uteis).</param>
public sealed record PedidoSicAberto(PedidoInformacaoSicId PedidoId, string Protocolo, DateOnly PrazoResposta) : IDomainEvent;

/// <summary>Pedido e-SIC entrou em atendimento.</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Protocolo">Protocolo.</param>
public sealed record PedidoSicEmAtendimento(PedidoInformacaoSicId PedidoId, string Protocolo) : IDomainEvent;

/// <summary>Prazo do pedido prorrogado (+10 dias uteis — LAI art. 11 §2o).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Protocolo">Protocolo.</param>
/// <param name="ProrrogadoAte">Novo prazo apos prorrogacao.</param>
public sealed record PedidoSicProrrogado(PedidoInformacaoSicId PedidoId, string Protocolo, DateOnly ProrrogadoAte) : IDomainEvent;

/// <summary>Pedido respondido (acesso concedido — LAI art. 11).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Protocolo">Protocolo.</param>
/// <param name="DataResposta">Data da resposta.</param>
/// <param name="EmAtraso">Indica se a resposta ocorreu apos o prazo vigente (indicador).</param>
public sealed record PedidoSicRespondido(PedidoInformacaoSicId PedidoId, string Protocolo, DateOnly DataResposta, bool EmAtraso) : IDomainEvent;

/// <summary>Pedido indeferido com fundamento legal (LAI art. 11 §1o).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Protocolo">Protocolo.</param>
/// <param name="Data">Data do indeferimento.</param>
public sealed record PedidoSicIndeferido(PedidoInformacaoSicId PedidoId, string Protocolo, DateOnly Data) : IDomainEvent;

/// <summary>Recurso interposto pelo cidadao (LAI art. 15).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Protocolo">Protocolo.</param>
/// <param name="Instancia">Instancia recorrida.</param>
/// <param name="DataInterposicao">Data de interposicao.</param>
public sealed record PedidoSicRecursoInterposto(PedidoInformacaoSicId PedidoId, string Protocolo, InstanciaRecurso Instancia, DateOnly DataInterposicao) : IDomainEvent;

/// <summary>Recurso decidido.</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Protocolo">Protocolo.</param>
/// <param name="Resultado">Resultado da decisao.</param>
/// <param name="DataDecisao">Data da decisao.</param>
public sealed record PedidoSicRecursoDecidido(PedidoInformacaoSicId PedidoId, string Protocolo, ResultadoRecurso Resultado, DateOnly DataDecisao) : IDomainEvent;

/// <summary>Pedido encerrado (terminal).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Protocolo">Protocolo.</param>
public sealed record PedidoSicEncerrado(PedidoInformacaoSicId PedidoId, string Protocolo) : IDomainEvent;
