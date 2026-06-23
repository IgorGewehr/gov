using System.Collections.Frozen;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>
/// Conjunto FECHADO (LG-A2) das hipoteses legais que podem legitimar o acesso a dado pessoal de
/// SAUDE (LGPD art. 11). Dado sensivel de saude so admite tratamento sob hipoteses do art. 11 —
/// jamais sob "consentimento" generico de dado comum: aqui modelamos as cabiveis no servico
/// publico de saude. Qualquer base legal fora deste conjunto ⇒ acesso negado e auditado.
/// </summary>
public static class BasesLegaisSaude
{
    /// <summary>Hipoteses legais aplicaveis ao acesso a dado de saude (art. 11 LGPD).</summary>
    public static readonly FrozenSet<BaseLegalLgpd> Aplicaveis = new[]
    {
        // Tutela da saude, em procedimento por profissional/servico de saude (art. 11, II, "f").
        BaseLegalLgpd.TutelaDaSaude,
        // Execucao de politica publica de saude (SUS) prevista em lei (art. 11, II, "b"; art. 23).
        BaseLegalLgpd.PoliticaPublica,
        // Cumprimento de obrigacao legal/regulatoria (ex.: notificacao compulsoria) (art. 11, II, "a").
        BaseLegalLgpd.ObrigacaoLegal,
    }.ToFrozenSet();
}
