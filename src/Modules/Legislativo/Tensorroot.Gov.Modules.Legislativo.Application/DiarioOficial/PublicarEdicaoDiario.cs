using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Contracts;
using Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;

namespace Tensorroot.Gov.Modules.Legislativo.Application.DiarioOficial;

/// <summary>
/// Publica a edicao do Diario Oficial (ato com efeito legal): define data/numero, emite o evento de
/// integracao via Outbox para o modulo Transparencia (LAI). Idempotente (republicar e no-op) — D-3.
/// </summary>
/// <param name="EdicaoId">Edicao a publicar.</param>
public sealed record PublicarEdicaoDiarioCommand(Guid EdicaoId) : ICommand;

/// <summary>Regras de validacao da publicacao.</summary>
public sealed class PublicarEdicaoDiarioValidator : AbstractValidator<PublicarEdicaoDiarioCommand>
{
    /// <summary>Define as regras.</summary>
    public PublicarEdicaoDiarioValidator() => RuleFor(comando => comando.EdicaoId).NotEmpty();
}

/// <summary>Handler da publicacao (publica o Integration Event ao Transparencia/LAI via Outbox).</summary>
public sealed class PublicarEdicaoDiarioHandler(
    IEdicaoDiarioRepository edicoes,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider) : ICommandHandler<PublicarEdicaoDiarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarEdicaoDiarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var edicao = await edicoes.ObterPorIdAsync(new EdicaoDiarioId(request.EdicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Edicao do Diario nao encontrada.");

        if (edicao.Publicada)
        {
            return; // D-3: idempotente — nao republica nem reemite o evento.
        }

        var agora = timeProvider.GetUtcNow();
        edicao.Publicar(agora, agora);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new DiarioOficialPublicadoIntegrationEvent(
            Guid.NewGuid(),
            agora.UtcDateTime,
            tenant.TenantId,
            edicao.Id.Value,
            edicao.Numero,
            edicao.Ano,
            edicao.DataPublicacao!.Value);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
