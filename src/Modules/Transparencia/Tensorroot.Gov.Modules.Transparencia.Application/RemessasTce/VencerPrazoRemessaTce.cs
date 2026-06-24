using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>
/// Sinaliza o vencimento do prazo legal de uma remessa não enviada (alerta de risco de bloqueio de
/// transferências — LRF art. 23 §3º). Acionado por Worker; idempotente (I-12).
/// </summary>
/// <param name="RemessaTceId">Identificador da remessa.</param>
public sealed record VencerPrazoRemessaTceCommand(Guid RemessaTceId) : ICommand;

/// <summary>Regras de validação do comando de vencimento de prazo.</summary>
public sealed class VencerPrazoRemessaTceValidator : AbstractValidator<VencerPrazoRemessaTceCommand>
{
    /// <summary>Define as regras.</summary>
    public VencerPrazoRemessaTceValidator() => RuleFor(comando => comando.RemessaTceId).NotEmpty();
}

/// <summary>Handler do alerta de prazo de remessa vencido.</summary>
public sealed class VencerPrazoRemessaTceHandler(
    IRemessaTceRepository remessas,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter integrationEvents,
    TimeProvider timeProvider)
    : ICommandHandler<VencerPrazoRemessaTceCommand>
{
    /// <inheritdoc />
    public async Task Handle(VencerPrazoRemessaTceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas.ObterPorIdAsync(new RemessaTceId(request.RemessaTceId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remessa não encontrada.");

        // TODO(fuso): trocar por IDataHojeTenant.Hoje() (prazo/data de dominio no fuso do tenant; ver W9 fix de fuso).
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        remessa.VencerPrazo(hoje);

        // Ponte Transparencia -> Painel do Gestor (M8): prazo vencido sem envio é risco de bloqueio de
        // transferências (LRF art. 23 §3º). Enfileirado no Outbox na MESMA transação — despacho isolado por
        // módulo ao drenar evita dois ModuleDbContext no escopo da requisição (guarda H5).
        var evento = new PrazoRemessaVencidoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            remessa.TenantId,
            remessa.Id.Value,
            remessa.Periodo.ToString(),
            remessa.DataLimite);
        integrationEvents.Enfileirar(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
