using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

namespace Tensorroot.Gov.Modules.Saude.Application.Vigilancia;

/// <summary>Reclassifica o ramo/risco de um estabelecimento fiscalizavel (mudanca de atividade).</summary>
/// <param name="EstabelecimentoId">Estabelecimento alvo.</param>
/// <param name="Ramo">Novo ramo.</param>
/// <param name="Risco">Novo grau de risco.</param>
public sealed record ReclassificarEstabelecimentoCommand(Guid EstabelecimentoId, RamoVisa Ramo, GrauRiscoSanitario Risco) : ICommand;

/// <summary>Handler da reclassificacao.</summary>
public sealed class ReclassificarEstabelecimentoHandler(IEstabelecimentoFiscalizavelRepository estabelecimentos, IUnitOfWork unitOfWork)
    : ICommandHandler<ReclassificarEstabelecimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReclassificarEstabelecimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(new EstabelecimentoFiscalizavelId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento fiscalizavel nao encontrado.");

        estabelecimento.Reclassificar(request.Ramo, request.Risco);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Interdita um estabelecimento fiscalizavel (medida da VISA — Lei 6.437/1977, art. 23).</summary>
/// <param name="EstabelecimentoId">Estabelecimento alvo.</param>
/// <param name="Motivo">Motivo da interdicao.</param>
public sealed record InterditarEstabelecimentoCommand(Guid EstabelecimentoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da interdicao.</summary>
public sealed class InterditarEstabelecimentoValidator : AbstractValidator<InterditarEstabelecimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public InterditarEstabelecimentoValidator()
    {
        RuleFor(c => c.EstabelecimentoId).NotEmpty();
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler da interdicao.</summary>
public sealed class InterditarEstabelecimentoHandler(IEstabelecimentoFiscalizavelRepository estabelecimentos, IUnitOfWork unitOfWork)
    : ICommandHandler<InterditarEstabelecimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(InterditarEstabelecimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(new EstabelecimentoFiscalizavelId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento fiscalizavel nao encontrado.");

        estabelecimento.Interditar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Levanta a interdicao de um estabelecimento (regularizacao).</summary>
/// <param name="EstabelecimentoId">Estabelecimento alvo.</param>
public sealed record LevantarInterdicaoCommand(Guid EstabelecimentoId) : ICommand;

/// <summary>Handler do levantamento de interdicao.</summary>
public sealed class LevantarInterdicaoHandler(IEstabelecimentoFiscalizavelRepository estabelecimentos, IUnitOfWork unitOfWork)
    : ICommandHandler<LevantarInterdicaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(LevantarInterdicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(new EstabelecimentoFiscalizavelId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento fiscalizavel nao encontrado.");

        estabelecimento.LevantarInterdicao();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
