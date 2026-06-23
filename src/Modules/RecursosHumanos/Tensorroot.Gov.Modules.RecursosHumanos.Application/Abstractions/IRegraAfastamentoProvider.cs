using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Resolve a <see cref="RegraAfastamento"/> vigente de um <see cref="TipoAfastamento"/> na competencia,
/// para o tenant atual. A implementacao consulta primeiro o catalogo PERSISTIDO de regras do tenant
/// (versionado por vigencia); na ausencia de regra cadastrada, cai para os DEFAULTS LEGAIS documentais
/// (parametrizados por configuracao — ex.: maternidade 120/180d conforme adesao), nunca <em>hardcoded</em>
/// no dominio (CLAUDE.md S7/S16).
/// </summary>
public interface IRegraAfastamentoProvider
{
    /// <summary>Obtem a regra vigente do tipo na competencia para o tenant atual.</summary>
    /// <param name="tipo">Tipo de afastamento.</param>
    /// <param name="competencia">Competencia de referencia (vigencia da regra).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A regra vigente (cadastrada ou default legal); nunca nula (fail-closed retorna o default).</returns>
    Task<RegraAfastamento> ObterVigenteAsync(TipoAfastamento tipo, Competencia competencia, CancellationToken cancellationToken);
}
