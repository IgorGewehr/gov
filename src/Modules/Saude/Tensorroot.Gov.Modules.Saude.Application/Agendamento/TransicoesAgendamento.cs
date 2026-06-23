using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using AgendamentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Agendamento.Agendamento;

namespace Tensorroot.Gov.Modules.Saude.Application.Agendamento;

/// <summary>Confirma o agendamento (Marcado → Confirmado).</summary>
/// <param name="AgendamentoId">Identificador do agendamento.</param>
public sealed record ConfirmarAgendamentoCommand(Guid AgendamentoId) : ICommand;

/// <summary>Cancela o agendamento e libera a vaga (convoca a fila de espera por prioridade).</summary>
/// <param name="AgendamentoId">Identificador do agendamento.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
/// <param name="Origem">Origem do cancelamento.</param>
public sealed record CancelarAgendamentoCommand(Guid AgendamentoId, string Motivo, OrigemCancelamento Origem) : ICommand;

/// <summary>Registra falta (paciente nao compareceu) e libera a vaga (convoca a fila).</summary>
/// <param name="AgendamentoId">Identificador do agendamento.</param>
public sealed record RegistrarFaltaCommand(Guid AgendamentoId) : ICommand;

/// <summary>Marca o agendamento como realizado, associando opcionalmente o Atendimento/PEP.</summary>
/// <param name="AgendamentoId">Identificador do agendamento.</param>
/// <param name="AtendimentoId">Atendimento (PEP) associado, quando houver.</param>
public sealed record RealizarAgendamentoCommand(Guid AgendamentoId, Guid? AtendimentoId) : ICommand;

/// <summary>Handler da confirmacao de agendamento.</summary>
public sealed class ConfirmarAgendamentoHandler(IAgendamentoRepository agendamentos, IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmarAgendamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConfirmarAgendamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agendamento = await agendamentos.ObterPorIdAsync(new AgendamentoId(request.AgendamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Agendamento nao encontrado.");

        agendamento.Confirmar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do cancelamento de agendamento (libera a vaga e convoca a fila de espera).</summary>
public sealed class CancelarAgendamentoHandler(
    IAgendamentoRepository agendamentos,
    IAgendaProfissionalRepository agendas,
    IFilaEsperaRepository filas,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CancelarAgendamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarAgendamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agendamento = await agendamentos.ObterPorIdAsync(new AgendamentoId(request.AgendamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Agendamento nao encontrado.");

        agendamento.Cancelar(request.Motivo, request.Origem);
        await LiberarVagaEConvocarAsync(agendas, filas, agendamento, timeProvider, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task LiberarVagaEConvocarAsync(
        IAgendaProfissionalRepository agendas,
        IFilaEsperaRepository filas,
        AgendamentoRaiz agendamento,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var agenda = await agendas.ObterPorIdAsync(agendamento.AgendaId, cancellationToken).ConfigureAwait(false);
        agenda?.LiberarVaga(agendamento.VagaId);

        // Convoca o proximo da fila para o mesmo profissional/estabelecimento/tipo, por prioridade + FIFO.
        var proximo = await filas
            .ObterProximoAConvocarAsync(agendamento.ProfissionalId, agendamento.EstabelecimentoId, agendamento.Tipo, cancellationToken)
            .ConfigureAwait(false);
        proximo?.Convocar(timeProvider.GetUtcNow());
    }
}

/// <summary>Handler do registro de falta (libera a vaga e convoca a fila de espera).</summary>
public sealed class RegistrarFaltaHandler(
    IAgendamentoRepository agendamentos,
    IAgendaProfissionalRepository agendas,
    IFilaEsperaRepository filas,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarFaltaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarFaltaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agendamento = await agendamentos.ObterPorIdAsync(new AgendamentoId(request.AgendamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Agendamento nao encontrado.");

        agendamento.RegistrarFalta(timeProvider.GetUtcNow());
        await CancelarAgendamentoHandler
            .LiberarVagaEConvocarAsync(agendas, filas, agendamento, timeProvider, cancellationToken)
            .ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da realizacao de agendamento (ponte para o Atendimento/PEP).</summary>
public sealed class RealizarAgendamentoHandler(IAgendamentoRepository agendamentos, IUnitOfWork unitOfWork)
    : ICommandHandler<RealizarAgendamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RealizarAgendamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agendamento = await agendamentos.ObterPorIdAsync(new AgendamentoId(request.AgendamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Agendamento nao encontrado.");

        agendamento.Realizar(request.AtendimentoId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
