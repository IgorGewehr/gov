using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Contracts;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Instala (abre) uma sessao Agendada com quorum atingido.</summary>
/// <param name="SessaoId">Sessao a abrir.</param>
public sealed record AbrirSessaoCommand(Guid SessaoId) : ICommand;

/// <summary>Regras de validacao da abertura de sessao.</summary>
public sealed class AbrirSessaoValidator : AbstractValidator<AbrirSessaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirSessaoValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
    }
}

/// <summary>Handler da abertura de sessao (publica <see cref="SessaoRealizadaIntegrationEvent"/>).</summary>
public sealed class AbrirSessaoHandler(
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<AbrirSessaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AbrirSessaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        sessao.Abrir();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new SessaoRealizadaIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            sessao.Id.Value,
            sessao.Tipo.ToString(),
            sessao.DataHora.Valor);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
