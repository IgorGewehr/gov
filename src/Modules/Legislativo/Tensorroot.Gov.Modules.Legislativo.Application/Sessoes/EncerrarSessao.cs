using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Contracts;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Encerra uma sessao Aberta ou Suspensa (terminal).</summary>
/// <param name="SessaoId">Sessao a encerrar.</param>
public sealed record EncerrarSessaoCommand(Guid SessaoId) : ICommand;

/// <summary>Regras de validacao do encerramento de sessao.</summary>
public sealed class EncerrarSessaoValidator : AbstractValidator<EncerrarSessaoCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarSessaoValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
    }
}

/// <summary>Handler do encerramento de sessao (publica <see cref="SessaoRealizadaIntegrationEvent"/>).</summary>
public sealed class EncerrarSessaoHandler(
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<EncerrarSessaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarSessaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        sessao.Encerrar();
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
