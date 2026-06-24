using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>
/// Fonte dos parametros do processo legislativo configuraveis por tenant (Regimento Interno).
/// Isola a Application da infraestrutura de configuracao (<c>IOptions</c>), mantendo a regra de
/// dependencia. O intersticio entre turnos da Emenda a LOM e parametrizavel (CLAUDE.md §7), nunca
/// um numero magico no codigo.
/// </summary>
public interface ILegislativoParametros
{
    /// <summary>
    /// Intersticio minimo exigido entre o primeiro e o segundo turno de votacao de materia de rito
    /// qualificado (Emenda a LOM), conforme o Regimento Interno do tenant.
    /// </summary>
    /// <returns>Intervalo minimo (em dias) entre turnos.</returns>
    Interstico IntersticioEntreTurnos();
}
