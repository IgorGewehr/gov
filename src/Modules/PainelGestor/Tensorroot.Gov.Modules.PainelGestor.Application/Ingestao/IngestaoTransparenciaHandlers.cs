using System.Globalization;
using System.Text.RegularExpressions;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;
using Tensorroot.Gov.Modules.Transparencia.Contracts;

namespace Tensorroot.Gov.Modules.PainelGestor.Application.Ingestao;

/// <summary>
/// ACL de entrada (Transparencia → Painel): consome <see cref="MinimoConstitucionalApuradoIntegrationEvent"/>
/// e SUBSTITUI os mínimos constitucionais setoriais (Saúde 15% / Educação 25%) do exercício. O cálculo é
/// da Transparencia; o Painel apenas materializa para exibir o semáforo. Idempotente (substitui).
/// </summary>
public sealed class ReceberMinimoConstitucionalHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<MinimoConstitucionalApuradoIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(MinimoConstitucionalApuradoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, notification.Exercicio, cancellationToken).ConfigureAwait(false);
        var linhas = notification.Setores.Select(setor => MinimoSetorialSnapshot.Criar(
            setor.Setor,
            setor.ReceitaBase,
            setor.Aplicado,
            setor.PercentualAplicado,
            setor.PercentualMinimo,
            setor.Situacao));
        snapshot.SubstituirMinimos(linhas);
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(MinimoConstitucionalApuradoIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// ACL de entrada (Transparencia → Painel): consome <see cref="RemessaEnviadaTceIntegrationEvent"/> e
/// incrementa o contador de remessas transmitidas ao TCE-RS no exercício (prontidão de prestação de
/// contas). Idempotente por <c>EventId</c> (contador — não pode contar a reentrega duas vezes).
/// </summary>
public sealed class ReceberRemessaEnviadaHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<RemessaEnviadaTceIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(RemessaEnviadaTceIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var exercicio = PeriodoParser.ExtrairExercicio(notification.Periodo, notification.DataEnvio.Year);
        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, exercicio, cancellationToken).ConfigureAwait(false);
        snapshot.RegistrarRemessaEnviada();
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(RemessaEnviadaTceIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// ACL de entrada (Transparencia → Painel): consome <see cref="PrazoRemessaVencidoIntegrationEvent"/> e
/// incrementa o contador de remessas com prazo vencido sem envio (risco de bloqueio de transferências
/// voluntárias — LRF art. 23 §3º). Idempotente por <c>EventId</c>.
/// </summary>
public sealed class ReceberPrazoRemessaVencidoHandler(
    MaterializadorIndicadores materializador,
    IIngestaoIdempotencia idempotencia,
    IUnitOfWork unitOfWork)
    : INotificationHandler<PrazoRemessaVencidoIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(PrazoRemessaVencidoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await idempotencia.JaProcessadoAsync(notification.EventId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var exercicio = PeriodoParser.ExtrairExercicio(notification.Periodo, notification.DataLimite.Year);
        var snapshot = await materializador.ObterOuCriarAsync(notification.TenantId, exercicio, cancellationToken).ConfigureAwait(false);
        snapshot.RegistrarPrazoRemessaVencido();
        idempotencia.Registrar(notification.EventId, notification.TenantId, nameof(PrazoRemessaVencidoIntegrationEvent));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Extrai o exercício (ano) de um texto livre de período (ex.: "1º Bimestre/2026", "2026", "2026-03").
/// Reprodutível e tolerante: pega o primeiro grupo de 4 dígitos no intervalo plausível; senão, usa o
/// fallback (ano da data do fato). Sem relógio.
/// </summary>
internal static partial class PeriodoParser
{
    public static int ExtrairExercicio(string? periodo, int fallback)
    {
        if (!string.IsNullOrWhiteSpace(periodo))
        {
            foreach (Match match in Quatro.Matches(periodo))
            {
                var ano = int.Parse(match.Value, CultureInfo.InvariantCulture);
                if (ano is >= 1988 and <= 9999)
                {
                    return ano;
                }
            }
        }

        return fallback;
    }

    [GeneratedRegex(@"\d{4}")]
    private static partial Regex QuatroRegex();

    private static readonly Regex Quatro = QuatroRegex();
}
