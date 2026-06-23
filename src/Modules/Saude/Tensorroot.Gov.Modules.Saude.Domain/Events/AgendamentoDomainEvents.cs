using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using Tensorroot.Gov.SharedKernel;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Events;

/// <summary>Agenda de disponibilidade do profissional aberta no estabelecimento.</summary>
/// <param name="AgendaId">Identificador da agenda.</param>
/// <param name="ProfissionalId">Profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
public sealed record AgendaProfissionalAberta(AgendaProfissionalId AgendaId, ProfissionalId ProfissionalId, EstabelecimentoId EstabelecimentoId) : IDomainEvent;

/// <summary>Agenda publicada: as vagas geradas tornaram-se marcaveis.</summary>
/// <param name="AgendaId">Identificador da agenda.</param>
/// <param name="VagasGeradas">Quantidade de vagas geradas/expandidas.</param>
public sealed record AgendaProfissionalPublicada(AgendaProfissionalId AgendaId, int VagasGeradas) : IDomainEvent;

/// <summary>Dia/horario da agenda bloqueado (indisponibilidade do profissional).</summary>
/// <param name="AgendaId">Identificador da agenda.</param>
/// <param name="Data">Data bloqueada.</param>
/// <param name="Motivo">Motivo do bloqueio.</param>
public sealed record DiaAgendaBloqueado(AgendaProfissionalId AgendaId, DateOnly Data, string Motivo) : IDomainEvent;

/// <summary>Dia/horario da agenda reaberto (vagas livres voltam ao pool).</summary>
/// <param name="AgendaId">Identificador da agenda.</param>
/// <param name="Data">Data reaberta.</param>
public sealed record DiaAgendaReaberto(AgendaProfissionalId AgendaId, DateOnly Data) : IDomainEvent;

/// <summary>Agendamento marcado (vaga ocupada).</summary>
/// <param name="AgendamentoId">Identificador do agendamento.</param>
/// <param name="PacienteId">Paciente.</param>
/// <param name="ProfissionalId">Profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="VagaId">Vaga ocupada.</param>
/// <param name="DataHora">Data/hora do atendimento agendado.</param>
public sealed record AgendamentoMarcado(
    AgendamentoId AgendamentoId,
    PacienteId PacienteId,
    ProfissionalId ProfissionalId,
    EstabelecimentoId EstabelecimentoId,
    VagaId VagaId,
    DateTimeOffset DataHora) : IDomainEvent;

/// <summary>Agendamento confirmado pelo paciente/unidade.</summary>
/// <param name="AgendamentoId">Identificador do agendamento.</param>
public sealed record AgendamentoConfirmado(AgendamentoId AgendamentoId) : IDomainEvent;

/// <summary>Agendamento cancelado (a vaga foi liberada).</summary>
/// <param name="AgendamentoId">Identificador do agendamento.</param>
/// <param name="VagaId">Vaga liberada.</param>
/// <param name="Origem">Origem do cancelamento.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record AgendamentoCancelado(AgendamentoId AgendamentoId, VagaId VagaId, OrigemCancelamento Origem, string Motivo) : IDomainEvent;

/// <summary>Falta registrada (paciente nao compareceu); a vaga foi liberada.</summary>
/// <param name="AgendamentoId">Identificador do agendamento.</param>
/// <param name="VagaId">Vaga liberada.</param>
public sealed record FaltaRegistrada(AgendamentoId AgendamentoId, VagaId VagaId) : IDomainEvent;

/// <summary>Agendamento realizado (paciente atendido); ponte para o Atendimento/PEP.</summary>
/// <param name="AgendamentoId">Identificador do agendamento.</param>
/// <param name="AtendimentoId">Atendimento (PEP) associado, quando houver.</param>
public sealed record AgendamentoRealizado(AgendamentoId AgendamentoId, Guid? AtendimentoId) : IDomainEvent;

/// <summary>Paciente inserido na fila de espera (sem vaga disponivel).</summary>
/// <param name="FilaEsperaId">Identificador da entrada na fila.</param>
/// <param name="PacienteId">Paciente.</param>
/// <param name="ProfissionalId">Profissional desejado (opcional).</param>
public sealed record PacienteIncluidoNaFilaDeEspera(FilaEsperaId FilaEsperaId, PacienteId PacienteId, ProfissionalId? ProfissionalId) : IDomainEvent;

/// <summary>Paciente convocado da fila de espera (vaga disponivel).</summary>
/// <param name="FilaEsperaId">Identificador da entrada na fila.</param>
public sealed record PacienteConvocadoDaFilaDeEspera(FilaEsperaId FilaEsperaId) : IDomainEvent;

/// <summary>Entrada da fila de espera removida (desistencia/obsoleta).</summary>
/// <param name="FilaEsperaId">Identificador da entrada na fila.</param>
/// <param name="Motivo">Motivo da remocao.</param>
public sealed record EntradaFilaDeEsperaRemovida(FilaEsperaId FilaEsperaId, string Motivo) : IDomainEvent;
