using System.Collections.Frozen;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;

/// <summary>
/// Conjunto FECHADO (LG-A2) das hipoteses legais que podem legitimar o acesso ao prontuario SUAS —
/// dado pessoal SENSIVEL (LGPD art. 11), frequentemente envolvendo CRIANCA/ADOLESCENTE (ECA). O
/// acesso e legitimado pela execucao da politica publica de assistencia social (SUAS), pela tutela
/// quando ha situacao de saude/risco, ou pelo exercicio regular de direitos em processo de protecao.
/// Qualquer base legal fora deste conjunto ⇒ acesso negado e auditado.
/// </summary>
public static class BasesLegaisAssistencia
{
    /// <summary>Hipoteses legais aplicaveis ao acesso ao prontuario SUAS (art. 11 LGPD).</summary>
    public static readonly FrozenSet<BaseLegalLgpd> Aplicaveis = new[]
    {
        // Execucao de politica publica de assistencia social (SUAS) prevista em lei (art. 11, II, "b"; art. 23).
        BaseLegalLgpd.PoliticaPublica,
        // Tutela da saude/vida do titular ou de terceiro (art. 11, II, "f").
        BaseLegalLgpd.TutelaDaSaude,
        // Exercicio regular de direitos em processo (ex.: medida de protecao a menor) (art. 11, II, "d").
        BaseLegalLgpd.ExercicioDeDireitos,
    }.ToFrozenSet();
}
