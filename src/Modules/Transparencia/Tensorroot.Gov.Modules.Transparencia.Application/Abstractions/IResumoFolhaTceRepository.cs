using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasFolha;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>
/// Repositorio do read model <see cref="ResumoFolhaTce"/> — snapshot da folha consumido do RH via
/// Integration Event (papel CONSUMIDOR; sempre tenant-scoped via Global Query Filter). A REMESSA DE FOLHA
/// ao TCE-RS e montada a partir deste resumo (I-13).
/// </summary>
public interface IResumoFolhaTceRepository
{
    /// <summary>Marca um novo resumo para insercao.</summary>
    /// <param name="resumo">Resumo a adicionar.</param>
    void Adicionar(ResumoFolhaTce resumo);

    /// <summary>
    /// Obtem o resumo pela folha de origem (idempotencia da ponte: um resumo por folha no tenant).
    /// </summary>
    /// <param name="folhaDePagamentoId">Folha de pagamento de origem (RH).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O resumo, ou <c>null</c> se inexistente no tenant.</returns>
    Task<ResumoFolhaTce?> ObterPorFolhaAsync(Guid folhaDePagamentoId, CancellationToken cancellationToken);

    /// <summary>Obtem o resumo mais recente de uma competencia (exercicio/mes) no tenant.</summary>
    /// <param name="exercicio">Ano de exercicio.</param>
    /// <param name="mes">Mes (1..12).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O resumo, ou <c>null</c> se inexistente.</returns>
    Task<ResumoFolhaTce?> ObterPorCompetenciaAsync(int exercicio, int mes, CancellationToken cancellationToken);
}
