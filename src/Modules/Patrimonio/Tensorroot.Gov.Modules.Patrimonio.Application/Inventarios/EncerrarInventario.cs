using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Encerra o inventário (exige conciliação prévia) e publica as recomendações via Outbox.</summary>
/// <param name="InventarioId">Inventário a encerrar.</param>
/// <param name="DataEncerramento">Data de encerramento.</param>
public sealed record EncerrarInventarioCommand(Guid InventarioId, DateOnly DataEncerramento) : ICommand;

/// <summary>Regras de validação do encerramento de inventário.</summary>
public sealed class EncerrarInventarioValidator : AbstractValidator<EncerrarInventarioCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarInventarioValidator() => RuleFor(comando => comando.InventarioId).NotEmpty();
}

/// <summary>Handler do encerramento de inventário.</summary>
public sealed class EncerrarInventarioHandler(
    IInventarioRepository inventarios,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<EncerrarInventarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarInventarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inventario = await inventarios.ObterPorIdAsync(new InventarioId(request.InventarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventário não encontrado.");

        inventario.Encerrar(request.DataEncerramento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new InventarioEncerradoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            inventario.Id.Value,
            inventario.Exercicio,
            inventario.Divergencias.Count);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
