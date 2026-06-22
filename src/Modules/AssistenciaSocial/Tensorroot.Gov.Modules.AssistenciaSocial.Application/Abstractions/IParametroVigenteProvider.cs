using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;

/// <summary>
/// Fornece os parametros versionados por vigencia/competencia (salario minimo e criterios),
/// parametrizaveis por tenant — nunca hardcoded (Beneficio I-1; CLAUDE.md secao 7). A
/// implementacao reside na Infraestrutura (tabela parametrizavel por tenant/vigencia).
/// </summary>
public interface IParametroVigenteProvider
{
    /// <summary>Obtem o salario minimo vigente na competencia informada.</summary>
    /// <param name="competencia">Competencia (ano/mes) de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O salario minimo vigente, ou <c>null</c> se nao parametrizado para a competencia (B-11).</returns>
    Task<ValorMonetario?> ObterSalarioMinimoVigenteAsync(Competencia competencia, CancellationToken cancellationToken);
}
