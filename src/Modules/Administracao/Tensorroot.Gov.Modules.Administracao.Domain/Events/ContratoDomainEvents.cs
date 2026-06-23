using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Events;

/// <summary>Contrato assinado/celebrado (situacao inicial <see cref="SituacaoContrato.Assinado"/>) — I-6.</summary>
/// <param name="ContratoId">Identificador do contrato.</param>
/// <param name="FornecedorId">Identificador do fornecedor contratado.</param>
/// <param name="Valor">Valor global original do contrato.</param>
public sealed record ContratoAssinado(ContratoId ContratoId, Guid FornecedorId, decimal Valor) : IDomainEvent;

/// <summary>Contrato publicado no PNCP — condicao de eficacia (Lei 14.133/2021, art. 174).</summary>
/// <param name="ContratoId">Identificador do contrato.</param>
/// <param name="NumeroContratoPncp">Identificador do contrato no PNCP.</param>
public sealed record ContratoPublicadoPncp(ContratoId ContratoId, string NumeroContratoPncp) : IDomainEvent;

/// <summary>Termo aditivo celebrado, respeitando o limite legal de alteracao (art. 125) — I-9/I-10.</summary>
/// <param name="ContratoId">Identificador do contrato.</param>
/// <param name="AditivoId">Identificador do aditivo celebrado.</param>
/// <param name="PercentualAcumulado">Percentual quantitativo acumulado apos o aditivo.</param>
public sealed record AditivoCelebrado(ContratoId ContratoId, Guid AditivoId, decimal PercentualAcumulado) : IDomainEvent;

/// <summary>
/// Contrato encerrado por termino da vigencia/conclusao do objeto (terminal). BUG-A7: distingue
/// encerramento normal (vigencia decorrida) de antecipado (antes do fim da vigencia).
/// </summary>
/// <param name="ContratoId">Identificador do contrato.</param>
/// <param name="Antecipado"><c>true</c> se encerrado antes do fim da vigencia (encerramento antecipado).</param>
public sealed record ContratoEncerrado(ContratoId ContratoId, bool Antecipado) : IDomainEvent;

/// <summary>Contrato rescindido (extincao antecipada com motivacao) — I-15.</summary>
/// <param name="ContratoId">Identificador do contrato.</param>
/// <param name="Motivo">Motivacao do ato administrativo de rescisao.</param>
public sealed record ContratoRescindido(ContratoId ContratoId, string Motivo) : IDomainEvent;
