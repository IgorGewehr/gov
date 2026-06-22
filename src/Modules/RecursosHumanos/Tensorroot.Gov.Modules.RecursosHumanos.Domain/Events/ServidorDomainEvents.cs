using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>Servidor admitido a partir de provimento em cargo (situacao inicial <c>Nomeado</c>); dispara o S-2200.</summary>
/// <param name="ServidorId">Identificador do servidor admitido.</param>
/// <param name="Matricula">Matricula unica do vinculo no tenant.</param>
/// <param name="CargoId">Cargo provido.</param>
public sealed record ServidorAdmitido(ServidorId ServidorId, Matricula Matricula, CargoId CargoId) : IDomainEvent;

/// <summary>Posse do servidor registrada dentro do prazo legal (situacao <c>Empossado</c>).</summary>
/// <param name="ServidorId">Identificador do servidor.</param>
/// <param name="DataPosse">Data da posse.</param>
public sealed record PosseRegistrada(ServidorId ServidorId, DateOnly DataPosse) : IDomainEvent;

/// <summary>Inicio efetivo do exercicio do servidor (situacao <c>EmExercicio</c>).</summary>
/// <param name="ServidorId">Identificador do servidor.</param>
/// <param name="DataExercicio">Data de inicio do exercicio.</param>
public sealed record ExercicioIniciado(ServidorId ServidorId, DateOnly DataExercicio) : IDomainEvent;

/// <summary>Estabilidade concedida ao servidor efetivo apos 3 anos de exercicio (situacao <c>Estavel</c>).</summary>
/// <param name="ServidorId">Identificador do servidor.</param>
/// <param name="DataEstabilidade">Data de aquisicao da estabilidade.</param>
public sealed record EstabilidadeConcedida(ServidorId ServidorId, DateOnly DataEstabilidade) : IDomainEvent;

/// <summary>Afastamento temporario do servidor registrado (situacao <c>Afastado</c>); dispara o S-2230.</summary>
/// <param name="ServidorId">Identificador do servidor.</param>
/// <param name="Inicio">Inicio do afastamento.</param>
/// <param name="Fim">Fim previsto do afastamento (nulo quando indeterminado).</param>
/// <param name="Motivo">Motivo do afastamento.</param>
public sealed record AfastamentoRegistrado(ServidorId ServidorId, DateOnly Inicio, DateOnly? Fim, string Motivo) : IDomainEvent;

/// <summary>Vinculo do servidor encerrado (situacao <c>Desligado</c>); dispara o S-2299.</summary>
/// <param name="ServidorId">Identificador do servidor.</param>
/// <param name="DataDesligamento">Data do desligamento.</param>
/// <param name="Motivo">Motivo do desligamento.</param>
public sealed record ServidorDesligado(ServidorId ServidorId, DateOnly DataDesligamento, string Motivo) : IDomainEvent;
