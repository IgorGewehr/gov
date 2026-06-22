using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.RestosAPagar;

/// <summary>Paga um Resto a Pagar.</summary>
/// <param name="RestoAPagarId">Identificador do resto a pagar.</param>
/// <param name="Valor">Valor a pagar.</param>
public sealed record PagarRestoAPagarCommand(Guid RestoAPagarId, decimal Valor) : ICommand;

/// <summary>Cancela parcela de um Resto a Pagar.</summary>
/// <param name="RestoAPagarId">Identificador do resto a pagar.</param>
/// <param name="Valor">Valor a cancelar.</param>
public sealed record CancelarRestoAPagarCommand(Guid RestoAPagarId, decimal Valor) : ICommand;

/// <summary>Regras de validação do pagamento de resto a pagar.</summary>
public sealed class PagarRestoAPagarValidator : AbstractValidator<PagarRestoAPagarCommand>
{
    /// <summary>Define as regras.</summary>
    public PagarRestoAPagarValidator() => RuleFor(c => c.Valor).GreaterThan(0m);
}

/// <summary>Regras de validação do cancelamento de resto a pagar.</summary>
public sealed class CancelarRestoAPagarValidator : AbstractValidator<CancelarRestoAPagarCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarRestoAPagarValidator() => RuleFor(c => c.Valor).GreaterThan(0m);
}

/// <summary>Handler do pagamento de resto a pagar.</summary>
public sealed class PagarRestoAPagarHandler(IRestoAPagarRepository restos, IUnitOfWork unitOfWork)
    : ICommandHandler<PagarRestoAPagarCommand>
{
    /// <inheritdoc />
    public async Task Handle(PagarRestoAPagarCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resto = await restos.ObterPorIdAsync(new RestoAPagarId(request.RestoAPagarId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Resto a Pagar nao encontrado.");

        resto.RegistrarPagamento(ValorMonetario.De(request.Valor));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do cancelamento de resto a pagar.</summary>
public sealed class CancelarRestoAPagarHandler(IRestoAPagarRepository restos, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarRestoAPagarCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarRestoAPagarCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resto = await restos.ObterPorIdAsync(new RestoAPagarId(request.RestoAPagarId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Resto a Pagar nao encontrado.");

        resto.Cancelar(ValorMonetario.De(request.Valor));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
