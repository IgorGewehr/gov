using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using AgendamentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Agendamento.Agendamento;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.PacienteId;

namespace Tensorroot.Gov.Modules.Saude.Application.Agendamento;

/// <summary>
/// Marca uma consulta/exame ocupando uma vaga LIVRE de uma agenda publicada. Orquestra as invariantes:
/// Paciente/Profissional/Estabelecimento existentes e ATIVOS (Onda 1); vaga livre (anti-overbooking);
/// sem duplo-agendamento do paciente no mesmo slot; data no futuro.
/// </summary>
/// <param name="PacienteId">Paciente.</param>
/// <param name="VagaId">Vaga (slot) a ocupar.</param>
/// <param name="Prioridade">Prioridade do agendamento.</param>
public sealed record MarcarAgendamentoCommand(
    Guid PacienteId,
    Guid VagaId,
    PrioridadeAgendamento Prioridade) : ICommand<Guid>;

/// <summary>Regras de validacao da marcacao.</summary>
public sealed class MarcarAgendamentoValidator : AbstractValidator<MarcarAgendamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public MarcarAgendamentoValidator()
    {
        RuleFor(comando => comando.PacienteId).NotEmpty().WithMessage("Paciente e obrigatorio.");
        RuleFor(comando => comando.VagaId).NotEmpty().WithMessage("Vaga e obrigatoria.");
        RuleFor(comando => comando.Prioridade).IsInEnum().WithMessage("Prioridade invalida.");
    }
}

/// <summary>Handler da marcacao de agendamento.</summary>
public sealed class MarcarAgendamentoHandler(
    IAgendaProfissionalRepository agendas,
    IAgendamentoRepository agendamentos,
    IPacienteRepository pacientes,
    IProfissionalCadastroRepository profissionais,
    IEstabelecimentoCadastroRepository estabelecimentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<MarcarAgendamentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(MarcarAgendamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vagaId = new VagaId(request.VagaId);
        var agenda = await agendas.ObterPorVagaAsync(vagaId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Vaga inexistente.");

        // Paciente existente e ATIVO.
        var pacienteId = new PacienteId(request.PacienteId);
        var paciente = await pacientes.ObterPorIdAsync(new Domain.Pacientes.PacienteId(request.PacienteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");
        if (paciente.Situacao is not Domain.Pacientes.SituacaoPaciente.Ativo)
        {
            throw new InvalidOperationException("Paciente inativo: nao pode ser agendado.");
        }

        // Profissional existente e ATIVO (Onda 1).
        var profissional = await profissionais.ObterPorIdAsync(agenda.ProfissionalId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Profissional nao encontrado.");
        if (profissional.Situacao is not Domain.Profissionais.SituacaoProfissional.Ativo)
        {
            throw new InvalidOperationException("Profissional inativo: nao admite marcacao.");
        }

        // Estabelecimento existente e ATIVO (Onda 1).
        var estabelecimento = await estabelecimentos.ObterPorIdAsync(agenda.EstabelecimentoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento nao encontrado.");
        if (!estabelecimento.EstaAtivo)
        {
            throw new InvalidOperationException("Estabelecimento inativo: nao admite marcacao.");
        }

        // Ocupa a vaga (anti-overbooking: exige a vaga LIVRE e a agenda ABERTA). Retorna a data/hora do slot.
        var dataHora = agenda.OcuparVaga(vagaId);

        // Data no futuro para marcar.
        var agora = timeProvider.GetUtcNow();
        if (dataHora < agora)
        {
            throw new InvalidOperationException("Nao e possivel marcar em uma vaga no passado.");
        }

        // Sem duplo-agendamento do paciente no mesmo instante.
        if (await agendamentos.PacienteTemConflitoAsync(pacienteId, dataHora, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Paciente ja possui agendamento ativo neste horario.");
        }

        var agendamento = AgendamentoRaiz.Marcar(
            tenant.TenantId,
            pacienteId,
            agenda.ProfissionalId,
            agenda.EstabelecimentoId,
            agenda.Id,
            vagaId,
            dataHora,
            agenda.Tipo,
            request.Prioridade,
            agora);

        agendamentos.Adicionar(agendamento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return agendamento.Id.Value;
    }
}
