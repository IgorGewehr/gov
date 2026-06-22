using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Financas): consome o evento de integracao
/// <see cref="MSCGeradaIntegrationEvent"/> publicado pelo modulo Financas (via Contracts) quando a
/// Matriz de Saldos Contabeis de uma competencia e gerada, disparando
/// <see cref="ConsolidarDeclaracaoFiscalCommand"/> para montar a declaracao do tipo MSC. Idempotente por
/// <c>EventId</c>/competencia (I-13: o reprocessamento nao cria declaracao duplicada).
/// </summary>
public sealed class ReceberMSCGeradaHandler(ISender sender, ILogger<ReceberMSCGeradaHandler> logger)
    : INotificationHandler<MSCGeradaIntegrationEvent>
{
    // TipoValor 3 = SaldoFinal (XBRL GL ending_balance) — e o saldo de fechamento da competencia que
    // compoe a declaracao MSC. Saldo inicial e movimento entram no detalhamento por TipoValor no M4.
    private const int TipoValorSaldoFinal = 3;

    /// <inheritdoc />
    public async Task Handle(MSCGeradaIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var linhas = notification.Linhas
            .Where(linha => linha.TipoValor == TipoValorSaldoFinal)
            .Select(linha => new LinhaContabilDto(
                linha.ContaPcasp,
                linha.NaturezaSaldo,
                linha.Valor,
                linha.InformacaoComplementar))
            .ToList();

        // Degradacao graciosa: uma MSC sem linhas de saldo final (competencia vazia) nao produz declaracao.
        // Enviar o comando vazio reprovaria na validacao (Linhas NotEmpty) e ENVENENARIA o Outbox (retry
        // infinito da mensagem). Logo: ack idempotente do evento sem consolidar. [ponte MSC->Transparencia]
        if (linhas.Count == 0)
        {
            logger.LogInformation(
                "MSC {Exercicio}/{Mes} sem saldos de fechamento; nada a consolidar (evento {EventId} reconhecido).",
                notification.Exercicio,
                notification.Mes,
                notification.EventId);
            return;
        }

        var comando = new ConsolidarDeclaracaoFiscalCommand(
            TipoDeclaracaoFiscal.Msc,
            notification.Exercicio,
            notification.Mes,
            null,
            null,
            linhas);

        await sender.Send(comando, cancellationToken).ConfigureAwait(false);
    }
}
