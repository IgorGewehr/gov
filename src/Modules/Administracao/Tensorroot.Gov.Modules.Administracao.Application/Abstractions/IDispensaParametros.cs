using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>
/// Fonte dos parametros da dispensa em razao do valor, configuraveis POR TENANT (sem numero magico no
/// codigo — CLAUDE.md §7/§16). Os limites do art. 75, I/II sao atualizados anualmente pelo IPCA-E
/// (art. 182; Dec. 12.807/2025 vigente em 2026, que revogou o Dec. 12.343/2024, e sucessores), entao a
/// quantidade/norma-fonte vem daqui — nunca literal no agregado. Isola a Application da infraestrutura de
/// configuracao (<c>IOptions</c>).
/// </summary>
public interface IDispensaParametros
{
    /// <summary>
    /// Limite de dispensa vigente para o fundamento informado (art. 75, I ou II), com a norma-fonte
    /// citavel para rastreabilidade.
    /// </summary>
    /// <param name="fundamento">Fundamento legal da dispensa em razao do valor.</param>
    /// <returns>Limite vigente e norma-fonte do limite.</returns>
    LimiteDispensa LimiteVigente(FundamentoDispensaValor fundamento);

    /// <summary>Prazo minimo de divulgacao do aviso de contratacao direta, em dias uteis (IN SEGES/ME 67/2021).</summary>
    /// <returns>Prazo minimo de divulgacao, em dias uteis.</returns>
    int PrazoMinimoDivulgacaoDiasUteis();
}

/// <summary>
/// Limite de dispensa vigente: <b>valor</b> em reais e <b>norma-fonte</b> citavel — reune o que o agregado
/// precisa para validar o teto sem que o numero/lei apareca solto no codigo (CLAUDE.md §7/§16).
/// </summary>
/// <param name="Valor">Valor-limite em reais.</param>
/// <param name="NormaFonte">Citacao legal (ex.: "Dec. 12.807/2025").</param>
public sealed record LimiteDispensa(decimal Valor, string NormaFonte);
