using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Resolve os <see cref="PercentuaisMargem"/> vigentes na competencia para o tenant atual (mesma filosofia
/// de <see cref="IParametrosFolhaProvider"/>/<see cref="IRegraAfastamentoProvider"/>): consulta primeiro o
/// catalogo PERSISTIDO de <c>ParametrosMargem</c> do tenant (versionado por vigencia); na ausencia, cai para
/// os DEFAULTS LEGAIS parametrizados (Lei 14.131/2021: 35% + 5% + 5%), nunca <em>hardcoded</em> no dominio
/// (CLAUDE.md S7/S16). Fail-closed: sempre retorna percentuais validos.
/// </summary>
public interface IParametrosMargemProvider
{
    /// <summary>Obtem os percentuais de margem vigentes na competencia para o tenant atual.</summary>
    /// <param name="competencia">Competencia de referencia (vigencia dos percentuais).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Percentuais por balde (cadastrados ou default legal); nunca nulo.</returns>
    Task<PercentuaisMargem> ObterVigenteAsync(Competencia competencia, CancellationToken cancellationToken);
}
