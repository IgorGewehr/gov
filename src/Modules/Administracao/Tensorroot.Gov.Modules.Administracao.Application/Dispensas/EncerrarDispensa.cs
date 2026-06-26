using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

namespace Tensorroot.Gov.Modules.Administracao.Application.Dispensas;

/// <summary>Declara a dispensa fracassada (sem cotacao valida/habilitada).</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="Motivo">Motivacao do ato.</param>
public sealed record DeclararDispensaFracassadaCommand(Guid DispensaId, string Motivo) : ICommand;

/// <summary>Declara a dispensa deserta (sem cotacoes/interessados).</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
public sealed record DeclararDispensaDesertaCommand(Guid DispensaId) : ICommand;

/// <summary>Revoga a dispensa por conveniencia/oportunidade.</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="Motivo">Motivacao do ato administrativo.</param>
public sealed record RevogarDispensaCommand(Guid DispensaId, string Motivo) : ICommand;

/// <summary>Anula a dispensa por ilegalidade.</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
/// <param name="Motivo">Motivacao do ato administrativo (vicio de legalidade).</param>
public sealed record AnularDispensaCommand(Guid DispensaId, string Motivo) : ICommand;

/// <summary>Regras de validacao da declaracao de fracasso.</summary>
public sealed class DeclararDispensaFracassadaValidator : AbstractValidator<DeclararDispensaFracassadaCommand>
{
    /// <summary>Define as regras.</summary>
    public DeclararDispensaFracassadaValidator()
    {
        RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(2000).WithMessage("Motivo e obrigatorio (max. 2000).");
    }
}

/// <summary>Regras de validacao da declaracao de deserta.</summary>
public sealed class DeclararDispensaDesertaValidator : AbstractValidator<DeclararDispensaDesertaCommand>
{
    /// <summary>Define as regras.</summary>
    public DeclararDispensaDesertaValidator()
        => RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
}

/// <summary>Regras de validacao da revogacao.</summary>
public sealed class RevogarDispensaValidator : AbstractValidator<RevogarDispensaCommand>
{
    /// <summary>Define as regras.</summary>
    public RevogarDispensaValidator()
    {
        RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(2000).WithMessage("Motivo e obrigatorio (max. 2000).");
    }
}

/// <summary>Regras de validacao da anulacao.</summary>
public sealed class AnularDispensaValidator : AbstractValidator<AnularDispensaCommand>
{
    /// <summary>Define as regras.</summary>
    public AnularDispensaValidator()
    {
        RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(2000).WithMessage("Motivo e obrigatorio (max. 2000).");
    }
}

/// <summary>Handler da declaracao de fracasso.</summary>
public sealed class DeclararDispensaFracassadaHandler(IDispensaRepository dispensas, IUnitOfWork unitOfWork)
    : ICommandHandler<DeclararDispensaFracassadaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DeclararDispensaFracassadaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");
        dispensa.DeclararFracassada(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da declaracao de deserta.</summary>
public sealed class DeclararDispensaDesertaHandler(IDispensaRepository dispensas, IUnitOfWork unitOfWork)
    : ICommandHandler<DeclararDispensaDesertaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DeclararDispensaDesertaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");
        dispensa.DeclararDeserta();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da revogacao.</summary>
public sealed class RevogarDispensaHandler(IDispensaRepository dispensas, IUnitOfWork unitOfWork)
    : ICommandHandler<RevogarDispensaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RevogarDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");
        dispensa.Revogar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da anulacao.</summary>
public sealed class AnularDispensaHandler(IDispensaRepository dispensas, IUnitOfWork unitOfWork)
    : ICommandHandler<AnularDispensaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AnularDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");
        dispensa.Anular(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
