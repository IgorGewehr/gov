using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Contracts;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Processos;

/// <summary>Tramita um processo para um setor de destino (movimentacao interna entre setores).</summary>
/// <param name="ProcessoId">Processo a tramitar.</param>
/// <param name="SetorDestinoId">Setor de destino.</param>
/// <param name="Observacao">Observacao opcional da tramitacao.</param>
public sealed record TramitarProcessoCommand(
    Guid ProcessoId,
    Guid SetorDestinoId,
    string? Observacao) : ICommand;

/// <summary>Regras de validacao da tramitacao de processo.</summary>
public sealed class TramitarProcessoValidator : AbstractValidator<TramitarProcessoCommand>
{
    /// <summary>Define as regras.</summary>
    public TramitarProcessoValidator()
    {
        RuleFor(comando => comando.ProcessoId).NotEmpty();
        RuleFor(comando => comando.SetorDestinoId).NotEmpty();
    }
}

/// <summary>Handler da tramitacao de processo.</summary>
public sealed class TramitarProcessoHandler(
    IProcessoRepository processos,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<TramitarProcessoCommand>
{
    /// <inheritdoc />
    public async Task Handle(TramitarProcessoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var processo = await processos.ObterPorIdAsync(new ProcessoId(request.ProcessoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        processo.Tramitar(request.SetorDestinoId, request.Observacao, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new ProcessoTramitadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            processo.Nup.Valor,
            request.SetorDestinoId);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
