namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Nivel de sensibilidade (ABAC do recurso, LGPD) de um dado — ENUM ORDENADO: cada nivel domina
/// o anterior. O acesso exige <c>clearance(sujeito) &gt;= sensibilidade(recurso)</c> e, para
/// <see cref="SensivelLGPD"/>, emissao obrigatoria de trilha de leitura.
/// </summary>
/// <remarks>
/// DECLARADO EM M1 APENAS PARA USO FUTURO (M1.x/M2): a aplicacao de clearance, a trilha de
/// leitura LGPD e os verbos <c>*.sensiveis.*</c> NAO fazem parte do M1-minimo. A ordem dos
/// membros (e seus valores) define a relacao de dominancia e deve ser preservada — comparar por
/// ordem (&lt;, &gt;), nunca por nome. Ver MODELO §2.5 e invariante I6.
/// </remarks>
public enum NivelSensibilidade
{
    /// <summary>Dado publico (ex.: Transparencia/LAI). Menor nivel.</summary>
    Publico = 0,

    /// <summary>Dado interno do ente (ex.: empenho). Acima de <see cref="Publico"/>.</summary>
    Interno = 1,

    /// <summary>Dado restrito (acesso por necessidade de servico). Acima de <see cref="Interno"/>.</summary>
    Restrito = 2,

    /// <summary>
    /// Dado pessoal sensivel sob LGPD (PEP Saude, prontuario SUAS, dado de menor na Educacao,
    /// sigilo fiscal). Maior nivel — sempre exige base legal e gera trilha de leitura.
    /// </summary>
    SensivelLGPD = 3,
}
