using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Publica o contrato no PNCP — condicao de eficacia (Lei 14.133/2021, art. 174).</summary>
/// <param name="ContratoId">Contrato a publicar.</param>
/// <param name="NumeroContratoPncp">Identificador atribuido pelo PNCP.</param>
public sealed record PublicarContratoNoPncpCommand(Guid ContratoId, string NumeroContratoPncp) : ICommand;

/// <summary>Regras de validacao da publicacao no PNCP.</summary>
public sealed class PublicarContratoNoPncpValidator : AbstractValidator<PublicarContratoNoPncpCommand>
{
    /// <summary>Define as regras.</summary>
    public PublicarContratoNoPncpValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty();
        RuleFor(comando => comando.NumeroContratoPncp).NotEmpty().MaximumLength(60);
    }
}

/// <summary>Handler da publicacao no PNCP (publica <see cref="ContratoPublicadoPncpIntegrationEvent"/>).</summary>
public sealed class PublicarContratoNoPncpHandler(
    IContratoRepository contratos,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<PublicarContratoNoPncpCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarContratoNoPncpCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contrato = await contratos.ObterPorIdAsync(new ContratoId(request.ContratoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato nao encontrado.");

        contrato.PublicarContratoPncp(request.NumeroContratoPncp);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new ContratoPublicadoPncpIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            contrato.Id.Value,
            request.NumeroContratoPncp);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
