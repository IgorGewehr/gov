using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Desliga um servidor (encerramento do vinculo); dispara o S-2299 e a revogacao (Acesso/Patrimonio).</summary>
/// <param name="ServidorId">Servidor a desligar.</param>
/// <param name="DataDesligamento">Data de encerramento do vinculo.</param>
/// <param name="Motivo">Motivo do desligamento.</param>
public sealed record DesligarServidorCommand(Guid ServidorId, DateOnly DataDesligamento, string Motivo) : ICommand;

/// <summary>Regras de validacao do desligamento.</summary>
public sealed class DesligarServidorValidator : AbstractValidator<DesligarServidorCommand>
{
    /// <summary>Define as regras.</summary>
    public DesligarServidorValidator()
    {
        RuleFor(comando => comando.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(comando => comando.DataDesligamento).NotEmpty().WithMessage("Data de desligamento e obrigatoria.");
        RuleFor(comando => comando.Motivo)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Motivo do desligamento e obrigatorio (max. 200 caracteres).");
    }
}

/// <summary>Handler do desligamento de servidor.</summary>
public sealed class DesligarServidorHandler(
    IServidorRepository servidores,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<DesligarServidorCommand>
{
    /// <inheritdoc />
    public async Task Handle(DesligarServidorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        servidor.Desligar(request.DataDesligamento, request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new ServidorDesligadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            request.ServidorId,
            request.DataDesligamento);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
