using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

/// <summary>
/// Fonte dos parâmetros de prazo do art. 94 §3 (Lei 14.133/2021) aplicáveis a OBRAS, configuráveis POR
/// TENANT (sem número mágico no código — CLAUDE.md §7/§16). O art. 94 §3 fixa, para obras, a
/// publicação/registro em até <b>25 dias úteis</b> após a assinatura e <b>45 dias úteis</b> após a
/// conclusão. Como a contagem é parametrizável (regulamento do ente + calendário de feriados municipal),
/// a quantidade/unidade/norma-fonte vem daqui — nunca literal no agregado. Espelha <c>IPncpParametros</c>
/// de Administração (W9.1): a porta vive na Application; a fonte (config hoje, tabela depois) fica na Infra.
/// </summary>
public interface IParametrosObraProvider
{
    /// <summary>
    /// Prazo de publicação/registro após a ASSINATURA do contrato de obra (art. 94 §3).
    /// Padrão legal: 25 dias úteis a partir da assinatura.
    /// </summary>
    /// <returns>Parâmetro (quantidade/unidade/norma-fonte) do prazo de assinatura.</returns>
    PrazoArt94Parametro PrazoAposAssinatura();

    /// <summary>
    /// Prazo de publicação/registro após a CONCLUSÃO da obra (art. 94 §3).
    /// Padrão legal: 45 dias úteis a partir da conclusão.
    /// </summary>
    /// <returns>Parâmetro (quantidade/unidade/norma-fonte) do prazo de conclusão.</returns>
    PrazoArt94Parametro PrazoAposConclusao();

    /// <summary>
    /// Antecedência (em dias úteis) com que um prazo do art. 94 §3 "a vencer" deve ser sinalizado ao
    /// Portal do Gestor (evento de alerta preventivo). Parametrizável por tenant.
    /// </summary>
    /// <returns>Janela de antecedência, em dias úteis, do alerta de prazo a vencer.</returns>
    int AntecedenciaAlertaDiasUteis();
}
