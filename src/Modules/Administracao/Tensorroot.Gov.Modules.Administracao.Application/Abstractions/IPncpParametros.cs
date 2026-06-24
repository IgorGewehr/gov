using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>
/// Fonte dos parametros de prazo do PNCP, configuraveis POR TENANT (sem numero magico no codigo —
/// CLAUDE.md §7/§16). A Lei 14.133/2021 art. 94 fixa a divulgacao no PNCP como condicao de eficacia do
/// contrato em ate <b>20 dias uteis</b> (instrumento) e o registro do extrato em ate <b>10 dias uteis</b>;
/// o art. 94 §3 trata de OBRAS (25/45 d.u.). Como a contagem e parametrizavel (a norma admite regulamento
/// proprio do ente e o calendario de feriados varia por municipio), a quantidade/unidade/norma-fonte vem
/// daqui — nunca literal no agregado. Isola a Application da infraestrutura de configuracao (<c>IOptions</c>),
/// mantendo a regra de dependencia.
/// </summary>
public interface IPncpParametros
{
    /// <summary>
    /// Prazo de DIVULGACAO do instrumento contratual no PNCP (condicao de eficacia — art. 94, caput).
    /// Padrao legal: 20 dias uteis a partir da assinatura.
    /// </summary>
    /// <returns>Quantidade, unidade e norma-fonte do prazo de divulgacao do contrato.</returns>
    ParametroPrazo Divulgacao();

    /// <summary>
    /// Prazo de divulgacao do EXTRATO/registro do contrato no PNCP (art. 94 — registro).
    /// Padrao legal: 10 dias uteis a partir da assinatura.
    /// </summary>
    /// <returns>Quantidade, unidade e norma-fonte do prazo de registro do extrato.</returns>
    ParametroPrazo RegistroExtrato();

    /// <summary>
    /// Antecedencia (em dias uteis) com que um prazo PNCP "a vencer" deve ser sinalizado ao Portal do
    /// Gestor (evento de alerta preventivo). Parametrizavel por tenant.
    /// </summary>
    /// <returns>Janela de antecedencia, em dias uteis, do alerta de prazo a vencer.</returns>
    int AntecedenciaAlertaDiasUteis();
}

/// <summary>
/// Triade de um prazo parametrizavel por tenant: <b>quantidade</b>, <b>unidade</b> (uteis|corridos) e
/// <b>norma-fonte</b> citavel. Reune o que o VO <see cref="PrazoLegal"/> precisa para resolver o vencimento
/// sem que o numero/lei apareca solto no codigo (CLAUDE.md §7/§16).
/// </summary>
/// <param name="Quantidade">Quantidade de dias (&gt;= 0).</param>
/// <param name="Unidade">Unidade de contagem (uteis|corridos).</param>
/// <param name="NormaFonte">Citacao legal (ex.: "Lei 14.133/2021 art. 94").</param>
public sealed record ParametroPrazo(int Quantidade, UnidadePrazo Unidade, string NormaFonte);
