using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Events;

/// <summary>MSC transmitida ao SICONFI (emitido na transmissao de declaracao do tipo Msc).</summary>
/// <param name="DeclaracaoFiscalId">Identificador da declaracao.</param>
/// <param name="Competencia">Competencia (MM/AAAA) da MSC.</param>
/// <param name="Protocolo">Protocolo retornado pelo SICONFI.</param>
public sealed record MscEnviadaSiconfi(DeclaracaoFiscalId DeclaracaoFiscalId, string Competencia, string Protocolo) : IDomainEvent;

/// <summary>Declaracao fiscal transmitida ao SICONFI (emitido na transmissao de declaracao dos tipos Rreo/Rgf/Dca).</summary>
/// <param name="DeclaracaoFiscalId">Identificador da declaracao.</param>
/// <param name="TipoDeclaracao">Especie do demonstrativo transmitido.</param>
/// <param name="Periodo">Periodo de referencia (competencia/bimestre/quadrimestre/exercicio).</param>
public sealed record DeclaracaoTransmitida(DeclaracaoFiscalId DeclaracaoFiscalId, string TipoDeclaracao, string Periodo) : IDomainEvent;

/// <summary>Declaracao fiscal homologada pelo SICONFI/STN.</summary>
/// <param name="DeclaracaoFiscalId">Identificador da declaracao.</param>
/// <param name="TipoDeclaracao">Especie do demonstrativo homologado.</param>
public sealed record DeclaracaoHomologada(DeclaracaoFiscalId DeclaracaoFiscalId, string TipoDeclaracao) : IDomainEvent;
