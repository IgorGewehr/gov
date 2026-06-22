using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Acesso (ACL/master data CNES) para validar estabelecimento e profissional ativos na competencia
/// do atendimento (I-1) e o vinculo de teleconsulta (CRM ativo — I-9). A implementacao concreta vive
/// na Infrastructure (sincrona ou em cache), com timeout/retry/circuit breaker (Polly).
/// </summary>
public interface IEstabelecimentoRepository
{
    /// <summary>Indica se o estabelecimento (CNES) esta ativo na competencia (I-1).</summary>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ativo na competencia.</returns>
    Task<bool> EstabelecimentoAtivoAsync(
        EstabelecimentoId estabelecimentoId,
        Competencia competencia,
        CancellationToken cancellationToken);

    /// <summary>Indica se o profissional/CBO esta ativo no estabelecimento na competencia (I-1).</summary>
    /// <param name="profissionalId">Profissional.</param>
    /// <param name="estabelecimentoId">Estabelecimento (CNES).</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o vinculo CBO estiver ativo na competencia.</returns>
    Task<bool> ProfissionalAtivoAsync(
        ProfissionalId profissionalId,
        EstabelecimentoId estabelecimentoId,
        Competencia competencia,
        CancellationToken cancellationToken);

    /// <summary>Indica se o profissional possui CRM ativo, habilitando teleconsulta (I-9; Lei 14.510/2022).</summary>
    /// <param name="profissionalId">Profissional.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se o profissional tiver CRM ativo.</returns>
    Task<bool> ProfissionalComCrmAtivoAsync(ProfissionalId profissionalId, CancellationToken cancellationToken);
}
