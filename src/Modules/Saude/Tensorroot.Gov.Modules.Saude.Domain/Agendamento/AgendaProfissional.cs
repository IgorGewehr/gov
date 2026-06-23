using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Agendamento;

/// <summary>
/// Grade de disponibilidade de um profissional num estabelecimento, para uma data, expandida em
/// <see cref="Vaga"/>s (slots) de duracao fixa. Raiz de agregado: nasce em <see cref="SituacaoAgenda.Rascunho"/>
/// via <see cref="Abrir"/>, gera as vagas e as torna marcaveis ao <see cref="Publicar"/>. As vagas sao
/// entidades owned — toda mutacao de slot ocorre por metodos do agregado (fronteira de consistencia da
/// concorrencia de marcacao/anti-overbooking). Reusa Profissional/Estabelecimento da Onda 1 por Id.
/// </summary>
public sealed class AgendaProfissional : AggregateRoot<AgendaProfissionalId>, IMustHaveTenant
{
    /// <summary>Duracao minima de um slot, em minutos.</summary>
    public const int DuracaoSlotMinima = 5;

    /// <summary>Capacidade minima de vagas por slot.</summary>
    public const int CapacidadeMinima = 1;

    /// <summary>Teto de vagas geradas por agenda (protege contra expansao abusiva).</summary>
    public const int TetoVagas = 500;

    private readonly List<Vaga> _vagas = [];

    private AgendaProfissional()
    {
    }

    private AgendaProfissional(
        AgendaProfissionalId id,
        Guid tenantId,
        ProfissionalId profissionalId,
        EstabelecimentoId estabelecimentoId,
        TipoAtendimentoAgenda tipo,
        DateOnly data,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        int duracaoSlotMinutos,
        int capacidadeVagas)
        : base(id)
    {
        TenantId = tenantId;
        ProfissionalId = profissionalId;
        EstabelecimentoId = estabelecimentoId;
        Tipo = tipo;
        Data = data;
        HoraInicio = horaInicio;
        HoraFim = horaFim;
        DuracaoSlotMinutos = duracaoSlotMinutos;
        CapacidadeVagas = capacidadeVagas;
        Situacao = SituacaoAgenda.Rascunho;
        RaiseDomainEvent(new AgendaProfissionalAberta(id, profissionalId, estabelecimentoId));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Profissional dono da grade (referencia por Id — Onda 1).</summary>
    public ProfissionalId ProfissionalId { get; private set; }

    /// <summary>Estabelecimento (CNES) da grade (referencia por Id — Onda 1).</summary>
    public EstabelecimentoId EstabelecimentoId { get; private set; }

    /// <summary>Natureza dos atendimentos da grade (consulta/exame).</summary>
    public TipoAtendimentoAgenda Tipo { get; private set; }

    /// <summary>Data da grade.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Hora de inicio do expediente.</summary>
    public TimeOnly HoraInicio { get; private set; }

    /// <summary>Hora de fim do expediente.</summary>
    public TimeOnly HoraFim { get; private set; }

    /// <summary>Duracao de cada slot, em minutos.</summary>
    public int DuracaoSlotMinutos { get; private set; }

    /// <summary>Capacidade (numero de vagas) por slot/horario.</summary>
    public int CapacidadeVagas { get; private set; }

    /// <summary>Situacao atual da grade.</summary>
    public SituacaoAgenda Situacao { get; private set; }

    /// <summary>Vagas (slots) geradas (somente leitura para fora do agregado).</summary>
    public IReadOnlyCollection<Vaga> Vagas => _vagas;

    /// <summary>
    /// Abre uma nova grade em <see cref="SituacaoAgenda.Rascunho"/>. As pre-condicoes de profissional/
    /// estabelecimento ATIVOS sao verificadas na orquestracao (handler/ACL), referenciados por Id.
    /// Emite <see cref="AgendaProfissionalAberta"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="profissionalId">Profissional.</param>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="tipo">Natureza (consulta/exame).</param>
    /// <param name="data">Data da grade.</param>
    /// <param name="horaInicio">Inicio do expediente.</param>
    /// <param name="horaFim">Fim do expediente.</param>
    /// <param name="duracaoSlotMinutos">Duracao de cada slot, em minutos (>= <see cref="DuracaoSlotMinima"/>).</param>
    /// <param name="capacidadeVagas">Vagas por slot (>= <see cref="CapacidadeMinima"/>).</param>
    /// <returns>Nova <see cref="AgendaProfissional"/> em rascunho.</returns>
    /// <exception cref="ArgumentException">Se a janela/duracao/capacidade for invalida.</exception>
    public static AgendaProfissional Abrir(
        Guid tenantId,
        ProfissionalId profissionalId,
        EstabelecimentoId estabelecimentoId,
        TipoAtendimentoAgenda tipo,
        DateOnly data,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        int duracaoSlotMinutos,
        int capacidadeVagas)
    {
        if (profissionalId.Value == Guid.Empty)
        {
            throw new ArgumentException("Profissional e obrigatorio.", nameof(profissionalId));
        }

        if (estabelecimentoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Estabelecimento (CNES) e obrigatorio.", nameof(estabelecimentoId));
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException($"Tipo de atendimento invalido: {tipo}.", nameof(tipo));
        }

        if (horaFim <= horaInicio)
        {
            throw new ArgumentException("A hora de fim deve ser posterior a hora de inicio.", nameof(horaFim));
        }

        if (duracaoSlotMinutos < DuracaoSlotMinima)
        {
            throw new ArgumentException($"Duracao do slot deve ser >= {DuracaoSlotMinima} minutos.", nameof(duracaoSlotMinutos));
        }

        if (capacidadeVagas < CapacidadeMinima)
        {
            throw new ArgumentException($"Capacidade deve ser >= {CapacidadeMinima}.", nameof(capacidadeVagas));
        }

        return new AgendaProfissional(
            AgendaProfissionalId.New(),
            tenantId,
            profissionalId,
            estabelecimentoId,
            tipo,
            data,
            horaInicio,
            horaFim,
            duracaoSlotMinutos,
            capacidadeVagas);
    }

    /// <summary>
    /// Expande a grade em vagas (slots) e publica a agenda (<see cref="SituacaoAgenda.Aberta"/>), tornando
    /// as vagas marcaveis. Idempotente quanto a multiplas chamadas: nao duplica vagas ja geradas. Emite
    /// <see cref="AgendaProfissionalPublicada"/>.
    /// </summary>
    /// <param name="fusoOffset">Offset de fuso aplicado a data/hora dos slots (ex.: -03:00 para o horario de Brasilia).</param>
    /// <exception cref="InvalidOperationException">Se a grade estiver bloqueada ou a expansao exceder <see cref="TetoVagas"/>.</exception>
    public void Publicar(TimeSpan fusoOffset)
    {
        if (Situacao == SituacaoAgenda.Bloqueada)
        {
            throw new InvalidOperationException("Agenda bloqueada nao pode ser publicada; reabra antes.");
        }

        if (_vagas.Count == 0)
        {
            var slots = ExpandirSlots(fusoOffset);
            if (slots.Count > TetoVagas)
            {
                throw new InvalidOperationException($"Expansao gera {slots.Count} vagas; teto e {TetoVagas}.");
            }

            _vagas.AddRange(slots);
        }

        Situacao = SituacaoAgenda.Aberta;
        RaiseDomainEvent(new AgendaProfissionalPublicada(Id, _vagas.Count));
    }

    /// <summary>
    /// Bloqueia o dia (indisponibilidade do profissional): bloqueia todas as vagas LIVRES; a grade passa a
    /// <see cref="SituacaoAgenda.Bloqueada"/>. Vagas ja OCUPADAS permanecem (os agendamentos exigem
    /// cancelamento explicito). Emite <see cref="DiaAgendaBloqueado"/>.
    /// </summary>
    /// <param name="motivo">Motivo do bloqueio (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    public void BloquearDia(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        foreach (var vaga in _vagas)
        {
            vaga.Bloquear();
        }

        Situacao = SituacaoAgenda.Bloqueada;
        RaiseDomainEvent(new DiaAgendaBloqueado(Id, Data, motivo.Trim()));
    }

    /// <summary>
    /// Reabre o dia bloqueado: as vagas bloqueadas voltam a <see cref="SituacaoVaga.Livre"/> e a grade a
    /// <see cref="SituacaoAgenda.Aberta"/>. Emite <see cref="DiaAgendaReaberto"/>.
    /// </summary>
    public void ReabrirDia()
    {
        foreach (var vaga in _vagas)
        {
            vaga.Reabrir();
        }

        Situacao = SituacaoAgenda.Aberta;
        RaiseDomainEvent(new DiaAgendaReaberto(Id, Data));
    }

    /// <summary>
    /// Ocupa uma vaga LIVRE da agenda (chamado pela orquestracao de marcacao). Garante anti-overbooking:
    /// a vaga deve estar livre e a agenda aberta. Retorna a data/hora ocupada para o agendamento.
    /// </summary>
    /// <param name="vagaId">Vaga a ocupar.</param>
    /// <returns>Data/hora da vaga ocupada.</returns>
    /// <exception cref="InvalidOperationException">Se a agenda nao estiver aberta, a vaga nao existir ou nao estiver livre.</exception>
    public DateTimeOffset OcuparVaga(VagaId vagaId)
    {
        if (Situacao != SituacaoAgenda.Aberta)
        {
            throw new InvalidOperationException($"Agenda nao esta aberta para marcacao. Situacao atual: {Situacao}.");
        }

        var vaga = _vagas.FirstOrDefault(v => v.Id == vagaId)
            ?? throw new InvalidOperationException("Vaga inexistente nesta agenda.");

        vaga.Ocupar();
        return vaga.DataHora;
    }

    /// <summary>Libera uma vaga (cancelamento/falta do agendamento). Idempotente quanto a vagas ja livres.</summary>
    /// <param name="vagaId">Vaga a liberar.</param>
    /// <exception cref="InvalidOperationException">Se a vaga nao existir nesta agenda.</exception>
    public void LiberarVaga(VagaId vagaId)
    {
        var vaga = _vagas.FirstOrDefault(v => v.Id == vagaId)
            ?? throw new InvalidOperationException("Vaga inexistente nesta agenda.");
        vaga.Liberar();
    }

    private List<Vaga> ExpandirSlots(TimeSpan fusoOffset)
    {
        var slots = new List<Vaga>();
        for (var hora = HoraInicio; hora < HoraFim; hora = hora.Add(TimeSpan.FromMinutes(DuracaoSlotMinutos)))
        {
            var dataHora = new DateTimeOffset(Data.Year, Data.Month, Data.Day, hora.Hour, hora.Minute, 0, fusoOffset);
            for (var vaga = 0; vaga < CapacidadeVagas; vaga++)
            {
                slots.Add(Vaga.Criar(dataHora));
            }
        }

        return slots;
    }
}
