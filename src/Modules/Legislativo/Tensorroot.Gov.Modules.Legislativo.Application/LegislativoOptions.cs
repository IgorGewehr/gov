namespace Tensorroot.Gov.Modules.Legislativo.Application;

/// <summary>
/// Parametros do processo legislativo configuraveis por tenant (Regimento Interno de cada Camara).
/// A CF/88 (art. 29, caput) e a LOM exigem DOIS turnos com intervalo para a Emenda a LOM, mas o
/// numero exato de dias do intersticio cabe ao Regimento — por isso e parametrizado, nunca um numero
/// magico no dominio (CLAUDE.md §7).
/// </summary>
public sealed class LegislativoOptions
{
    /// <summary>Secao de configuracao raiz do modulo Legislativo.</summary>
    public const string SecaoConfiguracao = "Legislativo";

    /// <summary>
    /// Intervalo minimo (em dias) entre o primeiro e o segundo turno de votacao de materia de rito
    /// qualificado (Emenda a LOM). Conforme o Regimento Interno do tenant. Nao negativo.
    /// </summary>
    public int IntersticioEntreTurnosDias { get; set; } = IntersticioPadraoDias;

    /// <summary>
    /// Intersticio padrao (1 dia) aplicado quando o tenant nao parametriza o seu Regimento — garante,
    /// no minimo, que os dois turnos NAO ocorram no mesmo dia (datas/sessoes distintas).
    /// </summary>
    public const int IntersticioPadraoDias = 1;
}
