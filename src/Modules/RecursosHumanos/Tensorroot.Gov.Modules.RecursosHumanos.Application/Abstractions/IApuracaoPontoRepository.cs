using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="ApuracaoPonto"/>.</summary>
public interface IApuracaoPontoRepository
{
    /// <summary>Marca uma nova apuracao para insercao.</summary>
    /// <param name="apuracao">Apuracao a adicionar.</param>
    void Adicionar(ApuracaoPonto apuracao);

    /// <summary>Remove uma apuracao (recalculo idempotente enquanto Aberta).</summary>
    /// <param name="apuracao">Apuracao a remover.</param>
    void Remover(ApuracaoPonto apuracao);

    /// <summary>Obtem a apuracao por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A apuracao, ou <c>null</c>.</returns>
    Task<ApuracaoPonto?> ObterPorIdAsync(ApuracaoPontoId id, CancellationToken cancellationToken);

    /// <summary>Obtem a apuracao de um servidor numa competencia (unica por servidor/competencia).</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A apuracao, ou <c>null</c>.</returns>
    Task<ApuracaoPonto?> ObterPorServidorCompetenciaAsync(Guid servidorId, Competencia competencia, CancellationToken cancellationToken);

    /// <summary>Lista as apuracoes de uma competencia no tenant (todos os servidores apurados).</summary>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Apuracoes da competencia.</returns>
    Task<IReadOnlyList<ApuracaoPonto>> ListarPorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken);

    /// <summary>Obtem o saldo do banco de horas APOS a apuracao anterior do servidor (mais recente antes da competencia).</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="competencia">Competencia corrente (busca anteriores a ela).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Saldo acumulado anterior (0 quando nao houver apuracao prévia).</returns>
    Task<int> ObterSaldoBancoHorasAnteriorAsync(Guid servidorId, Competencia competencia, CancellationToken cancellationToken);
}
