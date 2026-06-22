using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Contracts;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Atendimento;

/// <summary>Compartilha o RES do atendimento na RNDS (Bundle FHIR R4 via mTLS + ICP-Brasil).</summary>
/// <param name="AtendimentoId">Atendimento a compartilhar.</param>
public sealed record CompartilharAtendimentoNaRNDSCommand(Guid AtendimentoId) : ICommand;

/// <summary>Regras de validacao do compartilhamento na RNDS.</summary>
public sealed class CompartilharAtendimentoNaRNDSValidator : AbstractValidator<CompartilharAtendimentoNaRNDSCommand>
{
    /// <summary>Define as regras.</summary>
    public CompartilharAtendimentoNaRNDSValidator()
    {
        RuleFor(comando => comando.AtendimentoId).NotEmpty().WithMessage("Atendimento e obrigatorio.");
    }
}

/// <summary>Handler do compartilhamento na RNDS.</summary>
public sealed class CompartilharAtendimentoNaRNDSHandler(
    IAtendimentoRepository atendimentos,
    IRndsGateway rndsGateway,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<CompartilharAtendimentoNaRNDSCommand>
{
    /// <inheritdoc />
    public async Task Handle(CompartilharAtendimentoNaRNDSCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var atendimento = await atendimentos
            .ObterPorIdAsync(new AtendimentoId(request.AtendimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Atendimento nao encontrado.");

        // I-5: monta e envia o Bundle FHIR R4 via mTLS (idempotente por AtendimentoId).
        var protocoloRnds = await rndsGateway
            .EnviarBundleAsync(atendimento.Id, cancellationToken)
            .ConfigureAwait(false);

        atendimento.CompartilharNaRNDS(protocoloRnds);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new RESCompartilhadoNaRNDSIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            atendimento.Id.Value,
            protocoloRnds);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
