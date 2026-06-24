using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;

/// <summary>
/// Lançamento contábil automático da INSCRIÇÃO de Restos a Pagar no encerramento: ao consumir
/// <see cref="RestoAPagarInscrito"/>, aciona o roteiro conforme a classificação do RP —
/// <c>EVT-RP-INSC</c> (Não Processado: D Empenhado a Liquidar / C RP Não Processado) ou
/// <c>EVT-RP-INSC-PROC</c> (Processado: D Liquidado a Pagar / C RP Processado). Sem este handler, a
/// inscrição de RAP não aparece no controle contábil PCASP do encerramento e o Anexo de RAP / MSC de
/// encerramento subnotificam a dívida flutuante perante o TCE-RS.
/// </summary>
/// <remarks>
/// Idempotente por <c>RestoAPagarId</c> + roteiro. Reprodutível: a competência é derivada do exercício
/// de ORIGEM do empenho (31/12), nunca do relógio, fechando o RAP no exercício correto que se encerra.
/// </remarks>
public sealed class ContabilizarInscricaoRestoAPagarHandler(
    MotorContabil motor,
    IRestoAPagarRepository restos,
    IUnitOfWork unitOfWork) : INotificationHandler<RestoAPagarInscrito>
{
    /// <inheritdoc />
    public async Task Handle(RestoAPagarInscrito notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var resto = await restos.ObterPorIdAsync(notification.RestoAPagarId, cancellationToken).ConfigureAwait(false);
        if (resto is null)
        {
            return;
        }

        var fato = resto.Classificacao == ClassificacaoRestoAPagar.Processado
            ? FatoContabil.RestoAPagarProcessadoInscrito
            : FatoContabil.RestoAPagarInscrito;

        // Competência: o RAP fecha o exercício de ORIGEM (que se encerra), em 31/12 — derivada do
        // exercício persistido no agregado, sem relógio (reprodutível).
        var data = new DateOnly(resto.ExercicioOrigem, 12, 31);

        var gerados = await motor.ContabilizarAsync(
            fato,
            resto.ValorInscrito,
            data,
            notification.RestoAPagarId.Value,
            cancellationToken).ConfigureAwait(false);

        if (gerados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
