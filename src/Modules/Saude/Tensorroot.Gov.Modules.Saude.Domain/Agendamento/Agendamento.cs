using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Agendamento;

/// <summary>
/// Marcacao de uma consulta/exame de um Paciente com um Profissional num Estabelecimento, numa
/// <see cref="Vaga"/> da <see cref="AgendaProfissional"/>. Raiz de agregado: nasce <see cref="SituacaoAgendamento.Marcado"/>
/// via <see cref="Marcar"/> e transita por confirmacao/cancelamento/falta/realizacao, cada uma com evento
/// de dominio. A ocupacao/liberacao da vaga e coordenada pela orquestracao sobre o agregado agenda
/// (fronteira de consistencia). A consulta realizada faz ponte para o <see cref="Atendimento"/>/PEP.
/// Trata dado pessoal sensivel (LGPD art. 11) — trilha de acesso na leitura.
/// </summary>
public sealed class Agendamento : AggregateRoot<AgendamentoId>, IMustHaveTenant
{
    private Agendamento()
    {
    }

    private Agendamento(
        AgendamentoId id,
        Guid tenantId,
        PacienteId pacienteId,
        ProfissionalId profissionalId,
        EstabelecimentoId estabelecimentoId,
        AgendaProfissionalId agendaId,
        VagaId vagaId,
        DateTimeOffset dataHora,
        TipoAtendimentoAgenda tipo,
        PrioridadeAgendamento prioridade,
        DateTimeOffset dataMarcacao)
        : base(id)
    {
        TenantId = tenantId;
        PacienteId = pacienteId;
        ProfissionalId = profissionalId;
        EstabelecimentoId = estabelecimentoId;
        AgendaId = agendaId;
        VagaId = vagaId;
        DataHora = dataHora;
        Tipo = tipo;
        Prioridade = prioridade;
        DataMarcacao = dataMarcacao;
        Situacao = SituacaoAgendamento.Marcado;
        RaiseDomainEvent(new AgendamentoMarcado(id, pacienteId, profissionalId, estabelecimentoId, vagaId, dataHora));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Paciente agendado (referencia por Id — Onda 1).</summary>
    public PacienteId PacienteId { get; private set; }

    /// <summary>Profissional do atendimento (referencia por Id — Onda 1).</summary>
    public ProfissionalId ProfissionalId { get; private set; }

    /// <summary>Estabelecimento (CNES) do atendimento (referencia por Id — Onda 1).</summary>
    public EstabelecimentoId EstabelecimentoId { get; private set; }

    /// <summary>Agenda que originou a vaga (referencia por Id).</summary>
    public AgendaProfissionalId AgendaId { get; private set; }

    /// <summary>Vaga ocupada por este agendamento (referencia por Id).</summary>
    public VagaId VagaId { get; private set; }

    /// <summary>Data/hora do atendimento agendado.</summary>
    public DateTimeOffset DataHora { get; private set; }

    /// <summary>Natureza (consulta/exame).</summary>
    public TipoAtendimentoAgenda Tipo { get; private set; }

    /// <summary>Prioridade clinica/administrativa.</summary>
    public PrioridadeAgendamento Prioridade { get; private set; }

    /// <summary>Data/hora em que a marcacao foi feita.</summary>
    public DateTimeOffset DataMarcacao { get; private set; }

    /// <summary>Situacao atual no ciclo de vida da marcacao.</summary>
    public SituacaoAgendamento Situacao { get; private set; }

    /// <summary>Origem do cancelamento (nula ate cancelar).</summary>
    public OrigemCancelamento? OrigemCancelamento { get; private set; }

    /// <summary>Motivo do cancelamento (nulo ate cancelar).</summary>
    public string? MotivoCancelamento { get; private set; }

    /// <summary>Atendimento (PEP) associado ao realizar (nulo ate realizar com PEP).</summary>
    public Guid? AtendimentoId { get; private set; }

    /// <summary>Indica se o agendamento ainda esta ativo (ocupa a vaga).</summary>
    public bool EstaAtivo => Situacao is SituacaoAgendamento.Marcado or SituacaoAgendamento.Confirmado;

    /// <summary>
    /// Cria o agendamento ocupando a <paramref name="vagaId"/> (a ocupacao da vaga e feita pela orquestracao
    /// sobre o agregado agenda ANTES da criacao). As pre-condicoes de Paciente/Profissional/Estabelecimento
    /// existentes e ATIVOS e de data futura sao verificadas na orquestracao. Emite <see cref="AgendamentoMarcado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="pacienteId">Paciente.</param>
    /// <param name="profissionalId">Profissional.</param>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="agendaId">Agenda origem.</param>
    /// <param name="vagaId">Vaga ocupada.</param>
    /// <param name="dataHora">Data/hora do atendimento (da vaga).</param>
    /// <param name="tipo">Natureza (consulta/exame).</param>
    /// <param name="prioridade">Prioridade.</param>
    /// <param name="dataMarcacao">Data/hora da marcacao.</param>
    /// <returns>Novo <see cref="Agendamento"/> marcado.</returns>
    /// <exception cref="ArgumentException">Se algum identificador de referencia for vazio ou a prioridade/tipo for invalido.</exception>
    public static Agendamento Marcar(
        Guid tenantId,
        PacienteId pacienteId,
        ProfissionalId profissionalId,
        EstabelecimentoId estabelecimentoId,
        AgendaProfissionalId agendaId,
        VagaId vagaId,
        DateTimeOffset dataHora,
        TipoAtendimentoAgenda tipo,
        PrioridadeAgendamento prioridade,
        DateTimeOffset dataMarcacao)
    {
        if (pacienteId.Value == Guid.Empty)
        {
            throw new ArgumentException("Paciente e obrigatorio.", nameof(pacienteId));
        }

        if (profissionalId.Value == Guid.Empty)
        {
            throw new ArgumentException("Profissional e obrigatorio.", nameof(profissionalId));
        }

        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento (CNES) e obrigatorio.", nameof(estabelecimentoId));
        }

        if (vagaId.Value == Guid.Empty)
        {
            throw new ArgumentException("Vaga e obrigatoria.", nameof(vagaId));
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException($"Tipo de atendimento invalido: {tipo}.", nameof(tipo));
        }

        if (!Enum.IsDefined(prioridade))
        {
            throw new ArgumentException($"Prioridade invalida: {prioridade}.", nameof(prioridade));
        }

        return new Agendamento(
            AgendamentoId.New(),
            tenantId,
            pacienteId,
            profissionalId,
            estabelecimentoId,
            agendaId,
            vagaId,
            dataHora,
            tipo,
            prioridade,
            dataMarcacao);
    }

    /// <summary>Confirma a presenca (Marcado → Confirmado). Idempotente quando ja confirmado. Emite <see cref="AgendamentoConfirmado"/>.</summary>
    /// <exception cref="InvalidOperationException">Se o agendamento estiver encerrado (cancelado/falta/realizado).</exception>
    public void Confirmar()
    {
        if (Situacao == SituacaoAgendamento.Confirmado)
        {
            return;
        }

        if (Situacao != SituacaoAgendamento.Marcado)
        {
            throw new InvalidOperationException($"Confirmacao exige situacao Marcado. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoAgendamento.Confirmado;
        RaiseDomainEvent(new AgendamentoConfirmado(Id));
    }

    /// <summary>
    /// Cancela o agendamento ativo (Marcado/Confirmado → Cancelado). A liberacao da vaga e feita pela
    /// orquestracao sobre o agregado agenda. Emite <see cref="AgendamentoCancelado"/>.
    /// </summary>
    /// <param name="motivo">Motivo do cancelamento (obrigatorio).</param>
    /// <param name="origem">Origem do cancelamento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio ou a origem invalida.</exception>
    /// <exception cref="InvalidOperationException">Se o agendamento ja estiver encerrado.</exception>
    public void Cancelar(string motivo, OrigemCancelamento origem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (!Enum.IsDefined(origem))
        {
            throw new ArgumentException($"Origem de cancelamento invalida: {origem}.", nameof(origem));
        }

        if (!EstaAtivo)
        {
            throw new InvalidOperationException($"Agendamento nao pode ser cancelado. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoAgendamento.Cancelado;
        OrigemCancelamento = origem;
        MotivoCancelamento = motivo.Trim();
        RaiseDomainEvent(new AgendamentoCancelado(Id, VagaId, origem, MotivoCancelamento));
    }

    /// <summary>
    /// Registra falta (paciente nao compareceu): Marcado/Confirmado → Falta. So registravel na data do
    /// atendimento ou depois. A liberacao da vaga e feita pela orquestracao. Emite <see cref="FaltaRegistrada"/>.
    /// </summary>
    /// <param name="momento">Momento de referencia (agora).</param>
    /// <exception cref="InvalidOperationException">Se o agendamento estiver encerrado ou a data ainda nao chegou.</exception>
    public void RegistrarFalta(DateTimeOffset momento)
    {
        if (!EstaAtivo)
        {
            throw new InvalidOperationException($"Falta exige agendamento ativo. Situacao atual: {Situacao}.");
        }

        if (momento < DataHora)
        {
            throw new InvalidOperationException("Falta so e registravel na data do atendimento ou apos.");
        }

        Situacao = SituacaoAgendamento.Falta;
        RaiseDomainEvent(new FaltaRegistrada(Id, VagaId));
    }

    /// <summary>
    /// Marca o agendamento como realizado (paciente atendido): Marcado/Confirmado → Realizado, associando
    /// opcionalmente o <see cref="Atendimento"/>/PEP criado. Emite <see cref="AgendamentoRealizado"/>.
    /// </summary>
    /// <param name="atendimentoId">Atendimento (PEP) associado, quando houver.</param>
    /// <exception cref="InvalidOperationException">Se o agendamento estiver encerrado.</exception>
    public void Realizar(Guid? atendimentoId)
    {
        if (!EstaAtivo)
        {
            throw new InvalidOperationException($"Realizacao exige agendamento ativo. Situacao atual: {Situacao}.");
        }

        AtendimentoId = atendimentoId == Guid.Empty ? null : atendimentoId;
        Situacao = SituacaoAgendamento.Realizado;
        RaiseDomainEvent(new AgendamentoRealizado(Id, AtendimentoId));
    }
}
