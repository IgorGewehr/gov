using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Contracts;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

/// <summary>Apura o resultado anual de um diario aberto, fechando-o em Apurado (I-4/I-5).</summary>
/// <param name="DiarioClasseId">Diario a apurar.</param>
public sealed record ApurarResultadoCommand(Guid DiarioClasseId) : ICommand;

/// <summary>Regras de validacao da apuracao de resultado.</summary>
public sealed class ApurarResultadoValidator : AbstractValidator<ApurarResultadoCommand>
{
    /// <summary>Define as regras.</summary>
    public ApurarResultadoValidator()
    {
        RuleFor(comando => comando.DiarioClasseId).NotEmpty().WithMessage("Identificador do diario obrigatorio.");
    }
}

/// <summary>Handler da apuracao de resultado: apura, persiste e publica o evento de integracao.</summary>
public sealed class ApurarResultadoHandler(
    IDiarioClasseRepository diarios,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<ApurarResultadoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ApurarResultadoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diario = await diarios.ObterPorIdAsync(new DiarioClasseId(request.DiarioClasseId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Diario nao encontrado.");

        var resultado = diario.ApurarResultado();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new ResultadoApuradoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            diario.Id.Value,
            diario.MatriculaId.Value,
            resultado.ToString());

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
