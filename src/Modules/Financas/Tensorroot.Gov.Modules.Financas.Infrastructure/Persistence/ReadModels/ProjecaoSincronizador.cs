using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Sincroniza a projeção do balancete drenando, NO MESMO contexto, as mensagens de Outbox de
/// <see cref="LancamentoContabilRegistrado"/> ainda pendentes — fazendo o
/// <see cref="ProjetarBalanceteHandler"/> rodar de imediato. Marca cada mensagem como processada,
/// de modo que a drenagem assíncrona do Outbox (background) não reprocesse e NÃO haja dupla contagem
/// no balancete. Usado pelas fases do encerramento de exercício para ler saldos já atualizados.
/// </summary>
public sealed class ProjecaoSincronizador(
    FinancasDbContext context,
    TimeProvider timeProvider) : IProjecaoSincronizador
{
    private static readonly string TipoEventoProjecao =
        typeof(LancamentoContabilRegistrado).AssemblyQualifiedName ?? typeof(LancamentoContabilRegistrado).FullName!;

    /// <inheritdoc />
    public async Task<int> SincronizarBalanceteAsync(CancellationToken cancellationToken)
    {
        var handler = new ProjetarBalanceteHandler(
            new Repositories.LancamentoContabilRepository(context),
            new Repositories.ContaContabilRepository(context),
            new BalanceteProjection(context),
            context);

        var projetados = 0;
        var continuar = true;

        // Itera em lotes até esvaziar: o handler de projeção faz SaveChanges, então relê o pendente.
        while (continuar)
        {
            var pendentes = await context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null && m.DeadLetteredOnUtc == null && m.Type == TipoEventoProjecao)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(200)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (pendentes.Count == 0)
            {
                break;
            }

            foreach (var mensagem in pendentes)
            {
                var evento = JsonSerializer.Deserialize<LancamentoContabilRegistrado>(mensagem.Content)
                    ?? throw new InvalidOperationException("Evento de projecao desserializou para nulo.");

                await handler.Handle(evento, cancellationToken).ConfigureAwait(false);
                mensagem.ProcessedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
                projetados++;
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            continuar = pendentes.Count == 200;
        }

        return projetados;
    }
}
