using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Incorpora um novo bem ao acervo (variação patrimonial aumentativa).</summary>
/// <param name="Descricao">Descrição do bem.</param>
/// <param name="Tipo">Tipo do bem (1 = Móvel, 2 = Imóvel).</param>
/// <param name="ValorInicial">Valor de incorporação (custo de ingresso).</param>
/// <param name="ValorResidual">Resíduo estimado ao fim da vida útil.</param>
/// <param name="VidaUtilMeses">Vida útil em meses (maior que zero).</param>
/// <param name="DataIncorporacao">Data de ingresso ao acervo.</param>
/// <param name="Origem">Origem do ingresso (aquisição/doação/produção própria).</param>
public sealed record IncorporarBemCommand(
    string Descricao,
    int Tipo,
    decimal ValorInicial,
    decimal ValorResidual,
    int VidaUtilMeses,
    DateOnly DataIncorporacao,
    string Origem) : ICommand<Guid>;

/// <summary>Regras de validação da incorporação de bem.</summary>
public sealed class IncorporarBemValidator : AbstractValidator<IncorporarBemCommand>
{
    /// <summary>Define as regras.</summary>
    public IncorporarBemValidator()
    {
        RuleFor(comando => comando.Descricao).NotEmpty().MaximumLength(200);
        // Tipo e transportado como int no comando (DTO); valida contra os valores definidos de TipoBem.
        // (FluentValidation.IsInEnum() so funciona quando a propriedade JA e do tipo enum — em int falha sempre.)
        RuleFor(comando => comando.Tipo)
            .Must(tipo => Enum.IsDefined(typeof(TipoBem), tipo))
            .WithMessage("Tipo de bem invalido (1=Movel, 2=Imovel).");
        RuleFor(comando => comando.ValorInicial).GreaterThan(0);
        RuleFor(comando => comando.ValorResidual)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(comando => comando.ValorInicial);
        RuleFor(comando => comando.VidaUtilMeses).GreaterThan(0);
    }
}

/// <summary>Handler da incorporação de bem.</summary>
public sealed class IncorporarBemHandler(
    IBemPatrimonialRepository bens,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<IncorporarBemCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(IncorporarBemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = BemPatrimonial.Incorporar(
            tenant.TenantId,
            request.Descricao,
            (TipoBem)request.Tipo,
            ValorMonetario.De(request.ValorInicial),
            ValorMonetario.De(request.ValorResidual),
            request.VidaUtilMeses,
            request.DataIncorporacao,
            request.Origem);

        bens.Adicionar(bem);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new BemIncorporadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            bem.Id.Value,
            request.ValorInicial,
            request.Origem);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return bem.Id.Value;
    }
}
