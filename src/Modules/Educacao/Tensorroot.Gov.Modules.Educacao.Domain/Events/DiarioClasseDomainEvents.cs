using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Events;

/// <summary>Frequencia registrada em um diario de classe.</summary>
/// <param name="DiarioClasseId">Identificador do diario.</param>
/// <param name="Data">Data da frequencia registrada.</param>
public sealed record FrequenciaRegistrada(DiarioClasseId DiarioClasseId, DateOnly Data) : IDomainEvent;

/// <summary>Nota lancada em um diario de classe.</summary>
/// <param name="DiarioClasseId">Identificador do diario.</param>
/// <param name="Componente">Componente curricular da nota.</param>
/// <param name="Periodo">Periodo de avaliacao.</param>
public sealed record NotaLancada(DiarioClasseId DiarioClasseId, ComponenteCurricularId Componente, string Periodo) : IDomainEvent;

/// <summary>Resultado anual do diario apurado (alimenta a Situacao do Aluno — I-10).</summary>
/// <param name="DiarioClasseId">Identificador do diario.</param>
/// <param name="Resultado">Resultado apurado do aluno.</param>
public sealed record ResultadoApurado(DiarioClasseId DiarioClasseId, ResultadoAluno Resultado) : IDomainEvent;
