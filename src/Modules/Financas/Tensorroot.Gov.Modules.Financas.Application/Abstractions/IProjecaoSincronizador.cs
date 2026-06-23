namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>
/// Drena, de forma síncrona e na MESMA unidade de trabalho, os eventos de domínio pendentes no Outbox
/// do contexto de Finanças — fazendo a projeção do balancete (<c>ProjetarBalanceteHandler</c>) rodar
/// imediatamente. Usado pelo encerramento de exercício para que cada fase leia os saldos já
/// atualizados pela fase anterior (DESIGN §8), reusando o ÚNICO caminho de projeção (sem dupla
/// contagem) — a projeção do Outbox não reprocessa o que já foi marcado como processado.
/// </summary>
public interface IProjecaoSincronizador
{
    /// <summary>Drena o Outbox do contexto até esvaziar os eventos de projeção pendentes.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de eventos projetados.</returns>
    Task<int> SincronizarBalanceteAsync(CancellationToken cancellationToken);
}
