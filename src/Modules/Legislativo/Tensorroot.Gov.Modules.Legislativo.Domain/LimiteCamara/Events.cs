using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

/// <summary>Apuracao do art. 29-A aberta para um exercicio (situacao inicial: Rascunho).</summary>
/// <param name="ApuracaoId">Identificador da apuracao.</param>
/// <param name="Exercicio">Exercicio orcamentario sob teto.</param>
public sealed record ApuracaoArt29AAberta(ApuracaoArt29AId ApuracaoId, int Exercicio) : IDomainEvent;

/// <summary>
/// Apuracao do art. 29-A consolidada (demonstrativo fechado): registra os semaforos do teto e do subteto
/// de folha. Quando excedido, sinaliza risco de crime de responsabilidade (art. 29-A §2/§3) ao Portal.
/// </summary>
/// <param name="ApuracaoId">Identificador da apuracao.</param>
/// <param name="Exercicio">Exercicio orcamentario sob teto.</param>
/// <param name="SemaforoTeto">Semaforo do teto da despesa total.</param>
/// <param name="SemaforoFolha">Semaforo do subteto da folha (§1).</param>
public sealed record ApuracaoArt29AConsolidada(
    ApuracaoArt29AId ApuracaoId,
    int Exercicio,
    SemaforoLimite SemaforoTeto,
    SemaforoLimite SemaforoFolha) : IDomainEvent;
