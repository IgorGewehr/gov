namespace Tensorroot.Gov.Modules.Saude.Domain.Agendamento;

/// <summary>Natureza do atendimento agendado (governa o tipo de grade e marcacao).</summary>
public enum TipoAtendimentoAgenda
{
    /// <summary>Consulta (atendimento clinico).</summary>
    Consulta = 1,

    /// <summary>Exame (procedimento diagnostico).</summary>
    Exame = 2,
}

/// <summary>Situacao (estado) da grade de disponibilidade do profissional.</summary>
public enum SituacaoAgenda
{
    /// <summary>Em elaboracao; ainda nao gerou vagas marcaveis.</summary>
    Rascunho = 1,

    /// <summary>Publicada; as vagas estao disponiveis para marcacao.</summary>
    Aberta = 2,

    /// <summary>Bloqueada integralmente; nenhuma vaga admite nova marcacao.</summary>
    Bloqueada = 3,
}

/// <summary>Situacao (estado) de uma <see cref="Vaga"/> (slot) da agenda.</summary>
public enum SituacaoVaga
{
    /// <summary>Disponivel para marcacao.</summary>
    Livre = 1,

    /// <summary>Reservada transitoriamente (pre-marcacao). Reservada para uso futuro do fluxo de confirmacao em duas fases.</summary>
    Reservada = 2,

    /// <summary>Ocupada por um agendamento ativo.</summary>
    Ocupada = 3,

    /// <summary>Bloqueada (dia/horario indisponivel); nao admite marcacao.</summary>
    Bloqueada = 4,
}

/// <summary>Situacao (estado) do agendamento no ciclo de vida da marcacao.</summary>
public enum SituacaoAgendamento
{
    /// <summary>Marcado (estado inicial); a vaga foi ocupada.</summary>
    Marcado = 1,

    /// <summary>Confirmado pelo paciente/unidade.</summary>
    Confirmado = 2,

    /// <summary>Cancelado; a vaga foi liberada.</summary>
    Cancelado = 3,

    /// <summary>Falta (paciente nao compareceu); a vaga foi liberada — terminal.</summary>
    Falta = 4,

    /// <summary>Realizado (paciente atendido); ponte para o Atendimento/PEP — terminal.</summary>
    Realizado = 5,
}

/// <summary>Origem do cancelamento (auditoria/indicador de absenteismo).</summary>
public enum OrigemCancelamento
{
    /// <summary>Cancelado pelo paciente.</summary>
    Paciente = 1,

    /// <summary>Cancelado pela unidade/gestao.</summary>
    Unidade = 2,

    /// <summary>Cancelado pelo profissional.</summary>
    Profissional = 3,
}

/// <summary>Classificacao de prioridade do agendamento e da fila de espera (ordena a convocacao).</summary>
public enum PrioridadeAgendamento
{
    /// <summary>Eletiva (sem urgencia).</summary>
    Eletiva = 1,

    /// <summary>Prioritaria (idoso/gestante/condicao cronica).</summary>
    Prioritaria = 2,

    /// <summary>Urgente.</summary>
    Urgente = 3,
}

/// <summary>Situacao (estado) de uma entrada na fila de espera.</summary>
public enum SituacaoFilaEspera
{
    /// <summary>Aguardando vaga (estado inicial).</summary>
    Aguardando = 1,

    /// <summary>Convocado (vaga liberada/disponivel) — aguardando marcacao.</summary>
    Convocado = 2,

    /// <summary>Atendido (marcacao efetivada a partir da convocacao) — terminal.</summary>
    Atendido = 3,

    /// <summary>Removido da fila (desistencia/obsoleto) — terminal.</summary>
    Removido = 4,
}
