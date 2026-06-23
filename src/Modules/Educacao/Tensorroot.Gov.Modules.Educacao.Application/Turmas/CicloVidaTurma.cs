using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Turmas;

/// <summary>Abre a turma para enturmacao (Planejada -&gt; Aberta).</summary>
/// <param name="TurmaId">Identificador da turma.</param>
public sealed record AbrirTurmaCommand(Guid TurmaId) : ICommand;

/// <summary>Handler da abertura de turma.</summary>
public sealed class AbrirTurmaHandler(ITurmaRepository turmas, IUnitOfWork unitOfWork)
    : ICommandHandler<AbrirTurmaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AbrirTurmaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var turma = await turmas.ObterPorIdAsync(new TurmaId(request.TurmaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Turma nao encontrada.");
        turma.Abrir();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Encerra a turma (estado terminal). Exige ausencia de matriculados, salvo encerramento de ano letivo.</summary>
/// <param name="TurmaId">Identificador da turma.</param>
/// <param name="EncerramentoAnoLetivo">Se verdadeiro, permite encerrar com matriculados (fim do ano letivo).</param>
public sealed record EncerrarTurmaCommand(Guid TurmaId, bool EncerramentoAnoLetivo) : ICommand;

/// <summary>Handler do encerramento de turma.</summary>
public sealed class EncerrarTurmaHandler(ITurmaRepository turmas, IUnitOfWork unitOfWork)
    : ICommandHandler<EncerrarTurmaCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarTurmaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var turma = await turmas.ObterPorIdAsync(new TurmaId(request.TurmaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Turma nao encontrada.");
        turma.Encerrar(request.EncerramentoAnoLetivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Ajusta a capacidade de vagas de uma turma (nunca abaixo dos matriculados — I-T3).</summary>
/// <param name="TurmaId">Identificador da turma.</param>
/// <param name="Vagas">Nova capacidade de vagas.</param>
public sealed record AjustarVagasCommand(Guid TurmaId, int Vagas) : ICommand;

/// <summary>Regras de validacao do ajuste de vagas.</summary>
public sealed class AjustarVagasValidator : AbstractValidator<AjustarVagasCommand>
{
    /// <summary>Define as regras.</summary>
    public AjustarVagasValidator()
    {
        RuleFor(comando => comando.TurmaId).NotEmpty();
        RuleFor(comando => comando.Vagas).GreaterThan(0).WithMessage("Vagas deve ser maior que zero.");
    }
}

/// <summary>Handler do ajuste de vagas.</summary>
public sealed class AjustarVagasHandler(ITurmaRepository turmas, IUnitOfWork unitOfWork)
    : ICommandHandler<AjustarVagasCommand>
{
    /// <inheritdoc />
    public async Task Handle(AjustarVagasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var turma = await turmas.ObterPorIdAsync(new TurmaId(request.TurmaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Turma nao encontrada.");
        turma.AjustarVagas(request.Vagas);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
