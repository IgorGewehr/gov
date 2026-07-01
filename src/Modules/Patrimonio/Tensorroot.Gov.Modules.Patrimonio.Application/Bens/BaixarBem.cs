using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Baixa um bem do acervo (exige laudo/parecer e autorização — I-7).</summary>
/// <param name="BemPatrimonialId">Bem a baixar.</param>
/// <param name="MotivoBaixa">Motivo da baixa (enum).</param>
/// <param name="LaudoUri">Referência (URI) do laudo/parecer (obrigatório).</param>
/// <param name="AutorizacaoId">Identificador da autorização (obrigatório).</param>
public sealed record BaixarBemCommand(
    Guid BemPatrimonialId,
    MotivoBaixa MotivoBaixa,
    string LaudoUri,
    Guid AutorizacaoId) : ICommand;

/// <summary>Regras de validação da baixa de bem.</summary>
public sealed class BaixarBemValidator : AbstractValidator<BaixarBemCommand>
{
    /// <summary>Define as regras.</summary>
    public BaixarBemValidator()
    {
        RuleFor(comando => comando.BemPatrimonialId).NotEmpty();
        RuleFor(comando => comando.MotivoBaixa).IsInEnum();
        RuleFor(comando => comando.LaudoUri).NotEmpty();
        RuleFor(comando => comando.AutorizacaoId).NotEmpty();
    }
}

/// <summary>Handler da baixa de bem (o evento de domínio <c>BemBaixado</c> alimenta o Outbox para Finanças — I-8).</summary>
public sealed class BaixarBemHandler(
    IBemPatrimonialRepository bens,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    TimeProvider timeProvider)
    : ICommandHandler<BaixarBemCommand>
{
    /// <inheritdoc />
    public async Task Handle(BaixarBemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        // Enum já validado por IsInEnum() no validador; grava o NOME do motivo (legível) na trilha.
        var motivo = request.MotivoBaixa.ToString();
        bem.Baixar(motivo, request.LaudoUri, request.AutorizacaoId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new BemBaixadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            bem.TenantId,
            bem.Id.Value,
            motivo,
            bem.ValorContabil.Valor);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
