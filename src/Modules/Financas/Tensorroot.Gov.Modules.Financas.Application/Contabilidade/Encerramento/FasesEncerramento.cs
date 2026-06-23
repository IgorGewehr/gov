using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Encerramento;

/// <summary>
/// Reexecuta isoladamente a fase de apuração patrimonial (zera classes 3/4 — mês 13). Idempotente
/// por <c>OrigemReferenciaId</c>: reexecutar não duplica lançamentos (DESIGN §9, linha "fase isolada").
/// Não avança o status do agregado — destina-se a retomada/reconferência operacional.
/// </summary>
/// <param name="Exercicio">Exercício.</param>
public sealed record ApurarResultadoPatrimonialCommand(int Exercicio) : ICommand<int>;

/// <summary>Validação da apuração patrimonial isolada.</summary>
public sealed class ApurarResultadoPatrimonialValidator : AbstractValidator<ApurarResultadoPatrimonialCommand>
{
    /// <summary>Define as regras.</summary>
    public ApurarResultadoPatrimonialValidator() => RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(2000);
}

/// <summary>Handler da apuração patrimonial isolada.</summary>
public sealed class ApurarResultadoPatrimonialHandler(
    MotorEncerramento motor,
    IProjecaoSincronizador projecao,
    IUnitOfWork unitOfWork) : ICommandHandler<ApurarResultadoPatrimonialCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(ApurarResultadoPatrimonialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gerados = await motor.ApurarResultadoPatrimonialAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await projecao.SincronizarBalanceteAsync(cancellationToken).ConfigureAwait(false);
        return gerados;
    }
}

/// <summary>
/// Reexecuta isoladamente a abertura do exercício seguinte (transferência do resultado — mês 0).
/// Idempotente por <c>OrigemReferenciaId</c>.
/// </summary>
/// <param name="Exercicio">Exercício encerrado (a abertura ocorre em exercicio+1).</param>
public sealed record AbrirExercicioSeguinteCommand(int Exercicio) : ICommand<int>;

/// <summary>Validação da abertura isolada.</summary>
public sealed class AbrirExercicioSeguinteValidator : AbstractValidator<AbrirExercicioSeguinteCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirExercicioSeguinteValidator() => RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(2000);
}

/// <summary>Handler da abertura isolada.</summary>
public sealed class AbrirExercicioSeguinteHandler(
    MotorEncerramento motor,
    IProjecaoSincronizador projecao,
    IUnitOfWork unitOfWork) : ICommandHandler<AbrirExercicioSeguinteCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(AbrirExercicioSeguinteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gerados = await motor.AbrirExercicioSeguinteAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await projecao.SincronizarBalanceteAsync(cancellationToken).ConfigureAwait(false);
        return gerados;
    }
}
