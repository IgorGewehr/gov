using Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas.Lexml;
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

    /// <summary>
    /// Identificacao do ente (UF, municipio, autoridade padrao) para compor a URN/XML LexML-BR das
    /// normas do tenant (W9.5). Parametrizada por configuracao.
    /// </summary>
    /// <returns>Identificacao do ente normalizada na grafia LexML.</returns>
    /// <exception cref="InvalidOperationException">Se a jurisdicao do tenant nao estiver configurada.</exception>
    IdentificacaoEnte IdentificacaoEnte();

    /// <summary>
    /// Parametros do art. 29-A (faixas populacionais x percentual, subteto da folha §1, limiar de
    /// atencao e exercicio-corte da EC 109/2021), conforme a configuracao do tenant (norma-fonte).
    /// </summary>
    /// <returns>Parametros validados do art. 29-A.</returns>
    ParametrosArt29A ParametrosArt29A();
}
