namespace Tensorroot.Gov.Modules.Saude.Application.Agendamento;

/// <summary>Item da lista de vagas livres (consulta de disponibilidade).</summary>
/// <param name="AgendaId">Agenda dona da vaga.</param>
/// <param name="VagaId">Identificador da vaga.</param>
/// <param name="ProfissionalId">Profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="Tipo">Natureza (descricao).</param>
/// <param name="DataHora">Data/hora do slot.</param>
public sealed record VagaLivreItem(
    Guid AgendaId,
    Guid VagaId,
    Guid ProfissionalId,
    Guid EstabelecimentoId,
    string Tipo,
    DateTimeOffset DataHora);

/// <summary>Ficha completa da grade de disponibilidade.</summary>
/// <param name="Id">Identificador da agenda.</param>
/// <param name="ProfissionalId">Profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="Tipo">Natureza (descricao).</param>
/// <param name="Data">Data da grade.</param>
/// <param name="HoraInicio">Inicio do expediente.</param>
/// <param name="HoraFim">Fim do expediente.</param>
/// <param name="DuracaoSlotMinutos">Duracao de cada slot.</param>
/// <param name="CapacidadeVagas">Vagas por slot.</param>
/// <param name="Situacao">Situacao da grade (descricao).</param>
/// <param name="TotalVagas">Total de vagas geradas.</param>
/// <param name="VagasLivres">Vagas livres.</param>
public sealed record AgendaDetalhe(
    Guid Id,
    Guid ProfissionalId,
    Guid EstabelecimentoId,
    string Tipo,
    DateOnly Data,
    TimeOnly HoraInicio,
    TimeOnly HoraFim,
    int DuracaoSlotMinutos,
    int CapacidadeVagas,
    string Situacao,
    int TotalVagas,
    int VagasLivres);

/// <summary>Item da lista de agendamentos.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="PacienteId">Paciente.</param>
/// <param name="ProfissionalId">Profissional.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="DataHora">Data/hora do atendimento.</param>
/// <param name="Tipo">Natureza (descricao).</param>
/// <param name="Prioridade">Prioridade (descricao).</param>
/// <param name="Situacao">Situacao (descricao).</param>
public sealed record AgendamentoItemLista(
    Guid Id,
    Guid PacienteId,
    Guid ProfissionalId,
    Guid EstabelecimentoId,
    DateTimeOffset DataHora,
    string Tipo,
    string Prioridade,
    string Situacao);

/// <summary>Item da fila de espera (ordem de convocacao).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="PacienteId">Paciente.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="ProfissionalId">Profissional desejado (opcional).</param>
/// <param name="Especialidade">Especialidade/CBO desejada (opcional).</param>
/// <param name="Tipo">Natureza (descricao).</param>
/// <param name="Prioridade">Prioridade (descricao).</param>
/// <param name="DataEntrada">Data de entrada na fila.</param>
/// <param name="Situacao">Situacao na fila (descricao).</param>
public sealed record FilaEsperaItem(
    Guid Id,
    Guid PacienteId,
    Guid EstabelecimentoId,
    Guid? ProfissionalId,
    string? Especialidade,
    string Tipo,
    string Prioridade,
    DateTimeOffset DataEntrada,
    string Situacao);
