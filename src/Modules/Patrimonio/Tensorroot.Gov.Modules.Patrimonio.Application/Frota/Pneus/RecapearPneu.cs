using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota.Pneus;

/// <summary>Envia um pneu (removido) para recapagem.</summary>
/// <param name="PneuId">Pneu a recapar.</param>
public sealed record EnviarPneuParaRecapagemCommand(Guid PneuId) : ICommand;

/// <summary>Handler do envio para recapagem.</summary>
public sealed class EnviarPneuParaRecapagemHandler(IPneuRepository pneus, IUnitOfWork unitOfWork)
    : ICommandHandler<EnviarPneuParaRecapagemCommand>
{
    /// <inheritdoc />
    public async Task Handle(EnviarPneuParaRecapagemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pneu = await pneus.ObterPorIdAsync(new PneuId(request.PneuId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pneu não encontrado.");

        pneu.EnviarParaRecapagem();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Conclui a recapagem do pneu, restaurando o sulco e acrescendo o custo da reforma.</summary>
/// <param name="PneuId">Pneu em recapagem.</param>
/// <param name="SulcoRecapadoMilimetros">Sulco restaurado pela recapagem (mm).</param>
/// <param name="CustoRecapagem">Custo da recapagem.</param>
public sealed record ConcluirRecapagemCommand(
    Guid PneuId,
    decimal SulcoRecapadoMilimetros,
    decimal CustoRecapagem) : ICommand;

/// <summary>Regras de validação da conclusão de recapagem.</summary>
public sealed class ConcluirRecapagemValidator : AbstractValidator<ConcluirRecapagemCommand>
{
    /// <summary>Define as regras.</summary>
    public ConcluirRecapagemValidator()
    {
        RuleFor(comando => comando.PneuId).NotEmpty();
        RuleFor(comando => comando.SulcoRecapadoMilimetros).GreaterThan(0);
        RuleFor(comando => comando.CustoRecapagem).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler da conclusão de recapagem.</summary>
public sealed class ConcluirRecapagemHandler(IPneuRepository pneus, IUnitOfWork unitOfWork)
    : ICommandHandler<ConcluirRecapagemCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConcluirRecapagemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pneu = await pneus.ObterPorIdAsync(new PneuId(request.PneuId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pneu não encontrado.");

        pneu.ConcluirRecapagem(request.SulcoRecapadoMilimetros, ValorMonetario.De(request.CustoRecapagem));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Descarta/sucateia um pneu (fim de vida útil); estado terminal.</summary>
/// <param name="PneuId">Pneu a descartar.</param>
/// <param name="Motivo">Motivo do descarte.</param>
public sealed record DescartarPneuCommand(Guid PneuId, string Motivo) : ICommand;

/// <summary>Regras de validação do descarte de pneu.</summary>
public sealed class DescartarPneuValidator : AbstractValidator<DescartarPneuCommand>
{
    /// <summary>Define as regras.</summary>
    public DescartarPneuValidator()
    {
        RuleFor(comando => comando.PneuId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(200);
    }
}

/// <summary>Handler do descarte de pneu.</summary>
public sealed class DescartarPneuHandler(IPneuRepository pneus, IUnitOfWork unitOfWork)
    : ICommandHandler<DescartarPneuCommand>
{
    /// <inheritdoc />
    public async Task Handle(DescartarPneuCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pneu = await pneus.ObterPorIdAsync(new PneuId(request.PneuId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pneu não encontrado.");

        pneu.Descartar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
