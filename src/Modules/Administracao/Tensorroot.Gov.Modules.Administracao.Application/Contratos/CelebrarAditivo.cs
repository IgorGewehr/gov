using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Celebra um termo aditivo do contrato respeitando o limite legal (art. 125; I-9).</summary>
/// <remarks>
/// BUG-A1: o comando NAO recebe percentual — ele e derivado de <see cref="ValorDelta"/> sobre o valor
/// original DENTRO do agregado (fonte unica), impedindo que percentual e valor divirjam e furem o teto.
/// </remarks>
/// <param name="ContratoId">Contrato a aditar.</param>
/// <param name="Tipo">Tipo do aditivo.</param>
/// <param name="ValorDelta">Variacao de valor resultante (base do percentual nos quantitativos).</param>
/// <param name="NovaVigenciaFim">Nova data-fim de vigencia (para aditivo de prazo).</param>
/// <param name="Justificativa">Justificativa do aditivo.</param>
/// <param name="EhReforma">Indica reforma de edificio/equipamento (limite ampliado a 50%).</param>
public sealed record CelebrarAditivoCommand(
    Guid ContratoId,
    TipoAditivo Tipo,
    decimal ValorDelta,
    DateOnly? NovaVigenciaFim,
    string Justificativa,
    bool EhReforma = false) : ICommand<Guid>;

/// <summary>Regras de validacao da celebracao de aditivo.</summary>
public sealed class CelebrarAditivoValidator : AbstractValidator<CelebrarAditivoCommand>
{
    /// <summary>Define as regras.</summary>
    public CelebrarAditivoValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty().WithMessage("Contrato e obrigatorio.");
        RuleFor(comando => comando.Tipo).IsInEnum().WithMessage("Tipo de aditivo invalido.");
        // BUG-A1: o teto legal e validado no agregado sobre o valorDelta real; aqui so garantimos nao-negatividade.
        RuleFor(comando => comando.ValorDelta)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Valor do aditivo nao pode ser negativo.");
        RuleFor(comando => comando.Justificativa).NotEmpty().WithMessage("Justificativa do aditivo e obrigatoria.");
    }
}

/// <summary>Handler da celebracao de aditivo (publica <see cref="AditivoCeleradoIntegrationEvent"/>).</summary>
public sealed class CelebrarAditivoHandler(
    IContratoRepository contratos,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<CelebrarAditivoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CelebrarAditivoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contrato = await contratos.ObterPorIdAsync(new ContratoId(request.ContratoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato nao encontrado.");

        var aditivo = contrato.CelebrarAditivo(
            request.Tipo,
            ValorMonetario.De(request.ValorDelta),
            request.NovaVigenciaFim,
            request.Justificativa,
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
            request.EhReforma);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new AditivoCeleradoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            contrato.Id.Value,
            aditivo.Id.Value,
            contrato.ValorAtual.Valor,
            contrato.VigenciaFim);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return aditivo.Id.Value;
    }
}
