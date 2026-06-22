using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Anti-Corruption Layer (saida — sincrona) do SISREG (Sistema Nacional de Regulacao). Reserva e
/// liberacao de vaga sao idempotentes por <see cref="SolicitacaoRegulacaoId"/>, com timeout, retry e
/// circuit breaker (Polly) na implementacao de Infraestrutura. A reserva no SISREG e o consumo de
/// cota ocorrem na mesma transacao logica de autorizacao (compensacao no cancelamento).
/// </summary>
public interface ISisregGateway
{
    /// <summary>Reserva a vaga no SISREG para a solicitacao autorizada e retorna o protocolo.</summary>
    /// <param name="solicitacaoId">Identificador da solicitacao (chave de idempotencia).</param>
    /// <param name="procedimento">Procedimento SIGTAP autorizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Protocolo da reserva no SISREG.</returns>
    Task<string> ReservarVagaAsync(
        SolicitacaoRegulacaoId solicitacaoId,
        Procedimento procedimento,
        CancellationToken cancellationToken);

    /// <summary>Libera a reserva no SISREG ao cancelar uma solicitacao autorizada.</summary>
    /// <param name="solicitacaoId">Identificador da solicitacao (chave de idempotencia).</param>
    /// <param name="protocoloSisreg">Protocolo da reserva a liberar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa concluida ao liberar a reserva.</returns>
    Task LiberarReservaAsync(
        SolicitacaoRegulacaoId solicitacaoId,
        string protocoloSisreg,
        CancellationToken cancellationToken);
}
