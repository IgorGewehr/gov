using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Pasep;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;

/// <summary>PASEP apurado para a competencia (base = folha bruta; valor = base x aliquota).</summary>
/// <param name="ApuracaoId">Identificador da apuracao.</param>
/// <param name="Competencia">Competencia de referencia.</param>
/// <param name="BaseContribuicao">Base de calculo (folha bruta).</param>
/// <param name="Valor">Valor apurado da contribuicao.</param>
public sealed record PasepApurado(ApuracaoPasepId ApuracaoId, Competencia Competencia, decimal BaseContribuicao, decimal Valor) : IDomainEvent;
