using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Contracts;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Processos;

/// <summary>Arquiva um processo (terminal); a guarda passa a reger-se pela TTD/CONARQ.</summary>
/// <param name="ProcessoId">Processo a arquivar.</param>
/// <param name="Motivo">Motivo do arquivamento (opcional).</param>
public sealed record ArquivarProcessoCommand(Guid ProcessoId, string? Motivo) : ICommand;

/// <summary>Regras de validacao do arquivamento de processo.</summary>
public sealed class ArquivarProcessoValidator : AbstractValidator<ArquivarProcessoCommand>
{
    /// <summary>Define as regras.</summary>
    public ArquivarProcessoValidator()
    {
        RuleFor(comando => comando.ProcessoId).NotEmpty();
    }
}

/// <summary>Handler do arquivamento de processo.</summary>
public sealed class ArquivarProcessoHandler(
    IProcessoRepository processos,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<ArquivarProcessoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ArquivarProcessoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var processo = await processos.ObterPorIdAsync(new ProcessoId(request.ProcessoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        processo.Arquivar(request.Motivo, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new ProcessoArquivadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            processo.Nup.Valor);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
