using System.Collections.Frozen;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.MinhaFolha;

/// <summary>
/// Conjunto FECHADO (LG-A2) de hipoteses legais LGPD aplicaveis ao AUTOSSERVICO do servidor: o
/// proprio titular acessando os SEUS dados pessoais (contracheque, ponto, ferias, informe). Sao
/// dados pessoais COMUNS (art. 7), nao dados sensiveis do art. 11 — por isso o conjunto e o do art.
/// 7, e qualquer outra base e rejeitada e auditada pelo <c>TrilhaAcessoSensivelBehavior</c>.
/// </summary>
public static class BasesLegaisAutosservico
{
    /// <summary>
    /// Bases legais aplicaveis: exercicio regular de direitos do titular (acesso aos proprios dados —
    /// art. 7, VI; art. 18) e cumprimento de obrigacao legal do empregador (fornecer contracheque/
    /// informe de rendimentos — art. 7, II). Conjunto imutavel e ordenado.
    /// </summary>
    public static readonly FrozenSet<BaseLegalLgpd> Aplicaveis = new[]
    {
        BaseLegalLgpd.ExercicioDeDireitos,
        BaseLegalLgpd.ObrigacaoLegal,
    }.ToFrozenSet();
}
