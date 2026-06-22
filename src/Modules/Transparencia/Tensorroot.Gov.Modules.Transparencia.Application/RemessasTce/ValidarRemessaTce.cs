using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>Aplica o RDI do e-Validador a uma remessa <c>Gerada</c> (transita a <c>Validada</c> ou <c>Rejeitada</c>).</summary>
/// <param name="RemessaTceId">Identificador da remessa a validar.</param>
public sealed record ValidarRemessaTceCommand(Guid RemessaTceId) : ICommand;

/// <summary>Regras de validação do comando de validação de remessa.</summary>
public sealed class ValidarRemessaTceValidator : AbstractValidator<ValidarRemessaTceCommand>
{
    /// <summary>Define as regras.</summary>
    public ValidarRemessaTceValidator() => RuleFor(comando => comando.RemessaTceId).NotEmpty();
}

/// <summary>Handler da validação local (e-Validador / RDI) da remessa.</summary>
public sealed class ValidarRemessaTceHandler(
    IRemessaTceRepository remessas,
    IEValidadorTce eValidador,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    TimeProvider timeProvider)
    : ICommandHandler<ValidarRemessaTceCommand>
{
    /// <inheritdoc />
    public async Task Handle(ValidarRemessaTceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas.ObterPorIdAsync(new RemessaTceId(request.RemessaTceId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remessa não encontrada.");

        var rdi = await eValidador.ValidarAsync(remessa, cancellationToken).ConfigureAwait(false);
        remessa.RegistrarResultadoValidacao(rdi);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (remessa.Situacao == SituacaoRemessaTce.Rejeitada)
        {
            var evento = new RemessaRejeitadaIntegrationEvent(
                Guid.NewGuid(),
                timeProvider.GetUtcNow().UtcDateTime,
                remessa.TenantId,
                remessa.Id.Value,
                remessa.Periodo.ToString(),
                rdi.QuantidadeErros);

            await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
        }
    }
}
