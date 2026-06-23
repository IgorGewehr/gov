using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;

namespace Tensorroot.Gov.Modules.Saude.Application.Agendamento;

/// <summary>Publica a grade (expande as vagas e as torna marcaveis).</summary>
/// <param name="AgendaId">Identificador da agenda.</param>
public sealed record PublicarAgendaCommand(Guid AgendaId) : ICommand;

/// <summary>Bloqueia o dia da agenda (indisponibilidade do profissional).</summary>
/// <param name="AgendaId">Identificador da agenda.</param>
/// <param name="Motivo">Motivo do bloqueio.</param>
public sealed record BloquearDiaAgendaCommand(Guid AgendaId, string Motivo) : ICommand;

/// <summary>Reabre o dia da agenda bloqueado.</summary>
/// <param name="AgendaId">Identificador da agenda.</param>
public sealed record ReabrirDiaAgendaCommand(Guid AgendaId) : ICommand;

/// <summary>Handler da publicacao de agenda.</summary>
public sealed class PublicarAgendaHandler(IAgendaProfissionalRepository agendas, IUnitOfWork unitOfWork)
    : ICommandHandler<PublicarAgendaCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarAgendaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agenda = await agendas.ObterPorIdAsync(new AgendaProfissionalId(request.AgendaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Agenda nao encontrada.");

        agenda.Publicar(FusoAgenda.OffsetPadrao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do bloqueio de dia da agenda.</summary>
public sealed class BloquearDiaAgendaHandler(IAgendaProfissionalRepository agendas, IUnitOfWork unitOfWork)
    : ICommandHandler<BloquearDiaAgendaCommand>
{
    /// <inheritdoc />
    public async Task Handle(BloquearDiaAgendaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agenda = await agendas.ObterPorIdAsync(new AgendaProfissionalId(request.AgendaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Agenda nao encontrada.");

        agenda.BloquearDia(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da reabertura de dia da agenda.</summary>
public sealed class ReabrirDiaAgendaHandler(IAgendaProfissionalRepository agendas, IUnitOfWork unitOfWork)
    : ICommandHandler<ReabrirDiaAgendaCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReabrirDiaAgendaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agenda = await agendas.ObterPorIdAsync(new AgendaProfissionalId(request.AgendaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Agenda nao encontrada.");

        agenda.ReabrirDia();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
