using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto;

/// <summary>
/// Apura a jornada (PTRP) de um servidor numa competencia: trata as marcacoes do AFD (sem altera-lo),
/// computa trabalhado/extras/faltas contra a jornada e consolida a apuracao + saldo do banco de horas.
/// Idempotente: recria a apuracao Aberta se ja existir.
/// </summary>
/// <param name="ServidorId">Servidor a apurar.</param>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia (1 a 12).</param>
public sealed record ApurarJornadaCommand(Guid ServidorId, int Ano, int Mes) : ICommand<Guid>;

/// <summary>Regras de validacao da apuracao de jornada.</summary>
public sealed class ApurarJornadaValidator : AbstractValidator<ApurarJornadaCommand>
{
    /// <summary>Define as regras.</summary>
    public ApurarJornadaValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(c => c.Ano).InclusiveBetween(2000, 2100);
        RuleFor(c => c.Mes).InclusiveBetween(1, 12);
    }
}

/// <summary>Handler da apuracao de jornada (PTRP).</summary>
public sealed class ApurarJornadaHandler(
    IMarcacaoPontoRepository marcacoes,
    IJornadaTrabalhoRepository jornadas,
    IApuracaoPontoRepository apuracoes,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ApurarJornadaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ApurarJornadaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var competencia = Competencia.De(request.Ano, request.Mes);
        var primeiroDia = new DateOnly(request.Ano, request.Mes, 1);

        var jornada = await jornadas.ObterVigenteAsync(request.ServidorId, primeiroDia, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor sem jornada vigente na competencia.");

        // Recalculo idempotente: so recria se a apuracao anterior ainda estiver Aberta.
        var existente = await apuracoes.ObterPorServidorCompetenciaAsync(request.ServidorId, competencia, cancellationToken).ConfigureAwait(false);
        if (existente is { Situacao: SituacaoApuracaoPonto.Fechada })
        {
            throw new InvalidOperationException("Apuracao da competencia ja fechada; reabra para recalcular.");
        }

        if (existente is not null)
        {
            apuracoes.Remover(existente);
        }

        var doServidor = await marcacoes.ListarPorServidorCompetenciaAsync(request.ServidorId, competencia, cancellationToken).ConfigureAwait(false);
        var instantes = doServidor
            .Select(m => new InstanteMarcacao(m.DataHora, m.Sentido))
            .ToList();

        var resultado = TratamentoJornada.Apurar(instantes, jornada);
        var saldoAnterior = await apuracoes.ObterSaldoBancoHorasAnteriorAsync(request.ServidorId, competencia, cancellationToken).ConfigureAwait(false);

        var apuracao = ApuracaoPonto.Apurar(
            tenantContext.TenantId,
            request.ServidorId,
            competencia,
            resultado,
            saldoAnterior);

        apuracoes.Adicionar(apuracao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return apuracao.Id.Value;
    }
}
