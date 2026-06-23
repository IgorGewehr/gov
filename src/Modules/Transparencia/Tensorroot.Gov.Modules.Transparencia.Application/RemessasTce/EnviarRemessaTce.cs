using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>
/// Registra o PROTOCOLO/RECIBO retornado pelo PAD/e-Protocolo do TCE-RS (ato HUMANO de transmissão).
/// </summary>
/// <remarks>
/// O TCE-RS NÃO tem API de envio: a transmissão é MANUAL (PAD desktop + e-Protocolo + cert A3 pessoal).
/// Este comando NÃO faz POST a nenhum endpoint — apenas grava, auditado, o protocolo que o operador colou
/// do portal. Gated por <c>transparencia.remessa.transmitir</c> (Segregação de Funções).
/// </remarks>
/// <param name="RemessaTceId">Identificador da remessa transmitida.</param>
/// <param name="Protocolo">Protocolo/recibo retornado pelo portal.</param>
/// <param name="DataRecibo">Data do recibo.</param>
public sealed record RegistrarProtocoloTceCommand(Guid RemessaTceId, string Protocolo, DateOnly DataRecibo) : ICommand;

/// <summary>Regras de validação do registro de protocolo.</summary>
public sealed class RegistrarProtocoloTceValidator : AbstractValidator<RegistrarProtocoloTceCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarProtocoloTceValidator()
    {
        RuleFor(comando => comando.RemessaTceId).NotEmpty();
        RuleFor(comando => comando.Protocolo).NotEmpty().MaximumLength(120);
        RuleFor(comando => comando.DataRecibo).NotEmpty();
    }
}

/// <summary>Handler do registro do protocolo de transmissão (ProntaParaTransmissao → Enviada).</summary>
public sealed class RegistrarProtocoloTceHandler(
    IRemessaTceRepository remessas,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter integrationEvents,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarProtocoloTceCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarProtocoloTceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas
            .ObterPorIdAsync(new RemessaTceId(request.RemessaTceId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remessa não encontrada.");

        // Ato humano: grava o protocolo/recibo retornado pelo portal (sem POST de envio).
        remessa.RegistrarProtocolo(request.Protocolo, request.DataRecibo);

        // Ponte Transparencia -> Painel do Gestor (M8): a remessa transmitida ao TCE-RS conta na prontidão
        // de prestação de contas. Enfileirado no Outbox na MESMA transação — NUNCA publicado in-process: o
        // consumidor (PainelGestor) resolve o PainelGestorDbContext em escopo PRÓPRIO ao drenar
        // (ScopedOutboxMessageDispatcher), evitando dois ModuleDbContext no escopo da requisição (guarda H5).
        var evento = new RemessaEnviadaTceIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            remessa.TenantId,
            remessa.Id.Value,
            remessa.Periodo.ToString(),
            remessa.DataEnvio!.Value);
        integrationEvents.Enfileirar(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
