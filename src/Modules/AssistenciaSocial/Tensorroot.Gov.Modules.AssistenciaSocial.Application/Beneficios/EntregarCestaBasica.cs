using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Beneficios;

/// <summary>
/// Registra a entrega de cesta basica sobre um beneficio eventual concedido (Beneficio I-8).
/// Nao altera a situacao (permanece concedido). Publica
/// <see cref="CestaBasicaEntregueIntegrationEvent"/> (Outbox).
/// </summary>
/// <param name="BeneficioId">Identificador do beneficio.</param>
/// <param name="Quantidade">Quantidade de cestas (&gt;= 1).</param>
public sealed record EntregarCestaBasicaCommand(Guid BeneficioId, int Quantidade) : ICommand;

/// <summary>Regras de validacao da entrega de cesta basica.</summary>
public sealed class EntregarCestaBasicaValidator : AbstractValidator<EntregarCestaBasicaCommand>
{
    /// <summary>Define as regras.</summary>
    public EntregarCestaBasicaValidator()
    {
        RuleFor(comando => comando.BeneficioId)
            .NotEmpty()
            .WithMessage("Identificador do beneficio e obrigatorio.");

        RuleFor(comando => comando.Quantidade)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Quantidade de cestas deve ser >= 1.");
    }
}

/// <summary>Handler da entrega de cesta basica.</summary>
public sealed class EntregarCestaBasicaHandler(
    IBeneficioRepository beneficios,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider,
    IPublisher publisher)
    : ICommandHandler<EntregarCestaBasicaCommand>
{
    /// <inheritdoc />
    public async Task Handle(EntregarCestaBasicaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var beneficio = await beneficios.ObterPorIdAsync(new BeneficioId(request.BeneficioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Beneficio nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // I-8: exige beneficio Concedida do tipo Eventual; quantidade >= 1.
        beneficio.EntregarCestaBasica(request.Quantidade, hoje);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new CestaBasicaEntregueIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            beneficio.Id.Value,
            beneficio.FamiliaId,
            request.Quantidade,
            hoje);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
