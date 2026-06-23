using System.Collections.Frozen;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Cidadao.Application.MeusDados;

/// <summary>
/// Conjunto FECHADO (LG-A2) de hipoteses legais LGPD aplicaveis ao PORTAL DO CIDADAO: o proprio titular
/// acessando os SEUS dados (debitos/divida/2a via/processos). Analogo ao <c>BasesLegaisAutosservico</c>
/// do Minha Folha. Sao dados pessoais COMUNS (art. 7) — qualquer outra base e rejeitada e auditada pelo
/// <c>TrilhaAcessoSensivelBehavior</c> (deny-by-default da accountability).
/// </summary>
public static class BasesLegaisPortalCidadao
{
    /// <summary>
    /// Bases aplicaveis: exercicio regular de direitos do titular (acesso aos proprios dados — art. 7,
    /// VI; art. 18) e cumprimento de obrigacao legal do ente (fornecer 2a via/CDA/posicao — art. 7, II).
    /// </summary>
    public static readonly FrozenSet<BaseLegalLgpd> Aplicaveis = new[]
    {
        BaseLegalLgpd.ExercicioDeDireitos,
        BaseLegalLgpd.ObrigacaoLegal,
    }.ToFrozenSet();
}
