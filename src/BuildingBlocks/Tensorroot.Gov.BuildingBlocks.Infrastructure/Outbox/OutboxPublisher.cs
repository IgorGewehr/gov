using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

/// <summary>Publica as mensagens de Outbox pendentes — lado de publicação do Outbox Pattern.</summary>
public interface IOutboxPublisher
{
    /// <summary>Lê as mensagens pendentes do contexto, publica via MediatR e marca como processadas.</summary>
    /// <param name="context">Contexto (banco do tenant) cujo Outbox será drenado.</param>
    /// <param name="lote">Tamanho máximo do lote.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de mensagens publicadas com sucesso.</returns>
    Task<int> PublicarPendentesAsync(DbContext context, int lote, CancellationToken cancellationToken);
}

/// <summary>
/// Implementação do publicador de Outbox: deserializa cada mensagem pelo seu tipo e a despacha
/// aos handlers via <see cref="IOutboxMessageDispatcher"/>, marcando-a como processada. Resiliente
/// por mensagem (uma falha não interrompe o lote; o erro é registrado para reprocessamento).
/// <para>
/// A LEITURA do lote, a marcação de processado/dead-letter, o <see cref="OutboxMessage.AttemptCount"/>
/// e o <c>SaveChanges</c> ocorrem SEMPRE no contexto de leitura recebido. Já o DESPACHO de
/// cada mensagem é delegado ao dispatcher, que (em produção) o isola em um escopo de DI próprio por
/// mensagem — evitando que handlers de módulos distintos resolvam dois <c>ModuleDbContext</c> no mesmo
/// escopo (guarda H5). A idempotência at-least-once já existe nos handlers (ex.: MotorContábil por
/// OrigemReferenciaId), então um reprocessamento após falha parcial é seguro.
/// </para>
/// </summary>
public sealed class OutboxPublisher(IOutboxMessageDispatcher dispatcher, TimeProvider timeProvider) : IOutboxPublisher
{
    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Boundary de resiliência: a falha de uma mensagem não pode interromper o lote; o erro é registrado.")]
    public async Task<int> PublicarPendentesAsync(DbContext context, int lote, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var agora = timeProvider.GetUtcNow().UtcDateTime;

        // Elegíveis = ainda não processadas, NÃO dead-letter e cujo backoff já venceu. Uma poison
        // message recém-falhada tem NextAttemptUtc no futuro e fica FORA do lote, deixando a cabeça
        // livre para as mensagens válidas (não bloqueia mais o Take). Após o teto, vira dead-letter
        // e some permanentemente da seleção.
        var pendentes = await context.Set<OutboxMessage>()
            .Where(mensagem =>
                mensagem.ProcessedOnUtc == null
                && mensagem.DeadLetteredOnUtc == null
                && (mensagem.NextAttemptUtc == null || mensagem.NextAttemptUtc <= agora))
            .OrderBy(mensagem => mensagem.OccurredOnUtc)
            .Take(lote)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var processadas = 0;
        foreach (var mensagem in pendentes)
        {
            try
            {
                var tipo = Type.GetType(mensagem.Type)
                    ?? throw new InvalidOperationException($"Tipo do evento não resolvido: {mensagem.Type}");
                var evento = JsonSerializer.Deserialize(mensagem.Content, tipo)
                    ?? throw new InvalidOperationException("Conteúdo do evento desserializou para nulo.");

                // Despacho ISOLADO por mensagem: o dispatcher cria um escopo de DI próprio (com o tenant
                // da mensagem) e publica ali, garantindo que cada handler resolva só o contexto do SEU
                // módulo. O contexto de leitura permanece intocado para a contabilidade do Outbox.
                await dispatcher.DespacharAsync(evento, mensagem.TenantId, cancellationToken).ConfigureAwait(false);

                mensagem.ProcessedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
                mensagem.Error = null;
                processadas++;
            }
            catch (Exception excecao)
            {
                RegistrarFalha(mensagem, excecao, timeProvider.GetUtcNow().UtcDateTime);
            }
        }

        if (pendentes.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return processadas;
    }

    /// <summary>
    /// Contabiliza uma falha de processamento: incrementa o contador, registra o erro e ou agenda
    /// a próxima tentativa com backoff exponencial, ou — atingido o teto — envia para dead-letter.
    /// </summary>
    private static void RegistrarFalha(OutboxMessage mensagem, Exception excecao, DateTime agora)
    {
        mensagem.AttemptCount++;
        mensagem.Error = excecao.Message;

        if (mensagem.AttemptCount >= OutboxMessage.MaxAttempts)
        {
            // Veneno permanente: não reprocessa mais. Fica registrada com o erro para inspeção/alerta.
            mensagem.DeadLetteredOnUtc = agora;
            mensagem.NextAttemptUtc = null;
            return;
        }

        mensagem.NextAttemptUtc = agora + CalcularBackoff(mensagem.AttemptCount);
    }

    // Backoff exponencial com teto: 30s, 1min, 2min, 4min, ... (cap em ~15min) a partir da Nª falha.
    private static TimeSpan CalcularBackoff(int attemptCount)
    {
        var segundos = BackoffBaseSegundos * Math.Pow(2, attemptCount - 1);
        var limitado = Math.Min(segundos, BackoffMaxSegundos);
        return TimeSpan.FromSeconds(limitado);
    }

    private const double BackoffBaseSegundos = 30;
    private const double BackoffMaxSegundos = 15 * 60;
}
