using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Apura a BASE DE CALCULO da margem consignavel de um servidor na competencia: a remuneracao-base
/// consignavel = soma dos PROVENTOS da folha Mensal da competencia cujas rubricas tem
/// <c>RubricaConsignavel.ContaParaMargem</c> (tipicamente vencimento + permanentes; exclui eventuais).
/// A base e APURADA DA FOLHA, nunca digitada (design RH §2.2) — ja reflete o ajuste de afastamento (os
/// proventos da folha sao lancados ajustados pelo <c>AjustadorProventoPorAfastamento</c>). Quando ainda
/// nao ha folha da competencia, retorna a base do CARGO (vencimento) como estimativa para a averbacao.
/// </summary>
public interface IBaseConsignavelProvider
{
    /// <summary>Apura a base consignavel do servidor na competencia.</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Base consignavel (remuneracao-base que conta para a margem), nunca negativa.</returns>
    Task<decimal> ApurarBaseAsync(Guid servidorId, Competencia competencia, CancellationToken cancellationToken);
}
