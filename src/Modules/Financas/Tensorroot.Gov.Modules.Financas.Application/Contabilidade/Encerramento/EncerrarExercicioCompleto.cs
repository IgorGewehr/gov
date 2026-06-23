using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Msc;
using Tensorroot.Gov.Modules.Financas.Application.RestosAPagar;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Encerramento;

/// <summary>Resultado do encerramento completo de exercício (DESIGN §8).</summary>
/// <param name="Exercicio">Exercício encerrado.</param>
/// <param name="Status">Status final do agregado de encerramento.</param>
/// <param name="RestosAPagarInscritos">Restos a Pagar inscritos na fase 3.</param>
/// <param name="LancamentosPatrimoniais">Lançamentos de apuração patrimonial gerados.</param>
/// <param name="LancamentosOrcamentarios">Lançamentos de apuração orçamentária gerados.</param>
/// <param name="LancamentosAbertura">Lançamentos de abertura do exercício seguinte.</param>
/// <param name="JaEncerrado">Indica se o exercício já estava encerrado (idempotência/no-op).</param>
public sealed record EncerrarExercicioCompletoResultado(
    int Exercicio,
    StatusEncerramento Status,
    int RestosAPagarInscritos,
    int LancamentosPatrimoniais,
    int LancamentosOrcamentarios,
    int LancamentosAbertura,
    bool JaEncerrado);

/// <summary>
/// Encerra um exercício de ponta a ponta (orquestração): inscrição de RAP (mês 12) → encerramento
/// parcial → apuração patrimonial → apuração orçamentária (mês 13) → congelamento → abertura do
/// exercício seguinte (mês 0). Idempotente: reexecutar um exercício já encerrado é no-op (DESIGN §5).
/// </summary>
/// <param name="Exercicio">Exercício a encerrar.</param>
public sealed record EncerrarExercicioCompletoCommand(int Exercicio) : ICommand<EncerrarExercicioCompletoResultado>;

/// <summary>Validação do encerramento completo.</summary>
public sealed class EncerrarExercicioCompletoValidator : AbstractValidator<EncerrarExercicioCompletoCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarExercicioCompletoValidator() => RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(2000);
}

/// <summary>
/// Handler da orquestração do encerramento. Cada fase é uma unidade de trabalho: SaveChanges →
/// sincroniza o balancete (drena o Outbox) → a fase seguinte lê os saldos atualizados. O agregado
/// <see cref="EncerramentoExercicio"/> registra cada transição (idempotência por status); o
/// <c>MotorEncerramento</c> é idempotente por <c>OrigemReferenciaId</c> (retomada após falha sem
/// duplicar). Ao final, gera a MSC de encerramento (mês 13).
/// </summary>
public sealed class EncerrarExercicioCompletoHandler(
    IEncerramentoExercicioRepository encerramentos,
    MotorEncerramento motor,
    IProjecaoSincronizador projecao,
    IUnitOfWork unitOfWork,
    ISender sender,
    ITenantContext tenant,
    TimeProvider timeProvider) : ICommandHandler<EncerrarExercicioCompletoCommand, EncerrarExercicioCompletoResultado>
{
    /// <inheritdoc />
    public async Task<EncerrarExercicioCompletoResultado> Handle(
        EncerrarExercicioCompletoCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var exercicio = request.Exercicio;
        var encerramento = await encerramentos.ObterPorExercicioAsync(exercicio, cancellationToken).ConfigureAwait(false);

        if (encerramento is { Congelado: true })
        {
            // Exercício já congelado: no-op. Garante apenas que a abertura do seguinte foi concluída.
            var aberturaExtra = await GarantirAberturaAsync(encerramento, exercicio, cancellationToken).ConfigureAwait(false);
            return new EncerrarExercicioCompletoResultado(
                exercicio, encerramento.Status, 0, 0, 0, aberturaExtra, JaEncerrado: true);
        }

        if (encerramento is null)
        {
            encerramento = EncerramentoExercicio.Iniciar(tenant.TenantId, exercicio, timeProvider.GetUtcNow().UtcDateTime);
            encerramentos.Adicionar(encerramento);
        }

        var rapInscritos = 0;
        var patrimoniais = 0;
        var orcamentarios = 0;

        // Fase 3 (mês 12): inscrição de RAP do exercício corrente. Entra na MSC AGREGADA de dezembro.
        if (encerramento.TentarAvancarPara(StatusEncerramento.RapInscrito))
        {
            rapInscritos = await sender.Send(new EncerrarExercicioCommand(exercicio), cancellationToken).ConfigureAwait(false);
            encerramento.RegistrarConclusaoDeFase(timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await projecao.SincronizarBalanceteAsync(cancellationToken).ConfigureAwait(false);
        }

        // Fase 4 (mês 13): encerramento parcial (reclassificações/ajustes).
        // TODO(validar-oficial): automacao plena dos ajustes curto/longo prazo e perdas/provisoes.
        if (encerramento.TentarAvancarPara(StatusEncerramento.EncerramentoParcial))
        {
            encerramento.RegistrarConclusaoDeFase(timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Fase 5 (mês 13): apuração do resultado patrimonial (zera classes 3 e 4).
        if (encerramento.TentarAvancarPara(StatusEncerramento.ApuracaoPatrimonial))
        {
            patrimoniais = await motor.ApurarResultadoPatrimonialAsync(exercicio, cancellationToken).ConfigureAwait(false);
            encerramento.RegistrarConclusaoDeFase(timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await projecao.SincronizarBalanceteAsync(cancellationToken).ConfigureAwait(false);
        }

        // Fase 6 (mês 13): apuração do resultado orçamentário (zera classes 5 e 6 de execução).
        if (encerramento.TentarAvancarPara(StatusEncerramento.ApuracaoOrcamentaria))
        {
            orcamentarios = await motor.ApurarResultadoOrcamentarioAsync(exercicio, cancellationToken).ConfigureAwait(false);
            encerramento.RegistrarConclusaoDeFase(timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await projecao.SincronizarBalanceteAsync(cancellationToken).ConfigureAwait(false);
        }

        // Congela o exercício e gera a MSC de encerramento (mês 13) — base da DCA anual.
        if (encerramento.TentarAvancarPara(StatusEncerramento.Encerrado))
        {
            encerramento.RegistrarConclusaoDeFase(timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await sender.Send(new GerarMscEncerramentoCommand(exercicio), cancellationToken).ConfigureAwait(false);
        }

        // Fase 7 (mês 0 do exercício+1): abertura — transposição do resultado.
        var aberturaLancamentos = await GarantirAberturaAsync(encerramento, exercicio, cancellationToken).ConfigureAwait(false);

        return new EncerrarExercicioCompletoResultado(
            exercicio,
            encerramento.Status,
            rapInscritos,
            patrimoniais,
            orcamentarios,
            aberturaLancamentos,
            JaEncerrado: false);
    }

    private async Task<int> GarantirAberturaAsync(
        EncerramentoExercicio encerramento,
        int exercicio,
        CancellationToken cancellationToken)
    {
        if (!encerramento.TentarAvancarPara(StatusEncerramento.AberturaConcluida))
        {
            return 0;
        }

        var gerados = await motor.AbrirExercicioSeguinteAsync(exercicio, cancellationToken).ConfigureAwait(false);
        encerramento.RegistrarConclusaoDeFase(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await projecao.SincronizarBalanceteAsync(cancellationToken).ConfigureAwait(false);
        return gerados;
    }
}
