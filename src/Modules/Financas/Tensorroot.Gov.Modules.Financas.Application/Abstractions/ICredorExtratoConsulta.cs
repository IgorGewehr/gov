namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Item do extrato do credor: um empenho com seus valores acumulados de execução.</summary>
/// <param name="EmpenhoId">Identificador do empenho.</param>
/// <param name="Numero">Número do empenho.</param>
/// <param name="Exercicio">Exercício.</param>
/// <param name="DataEmpenho">Data do empenho.</param>
/// <param name="Situacao">Situação do empenho.</param>
/// <param name="ValorEmpenhado">Valor empenhado.</param>
/// <param name="ValorAnulado">Valor anulado.</param>
/// <param name="ValorLiquidado">Valor liquidado acumulado.</param>
/// <param name="ValorPago">Valor pago acumulado.</param>
public sealed record CredorExtratoItem(
    Guid EmpenhoId,
    string Numero,
    int Exercicio,
    DateOnly DataEmpenho,
    string Situacao,
    decimal ValorEmpenhado,
    decimal ValorAnulado,
    decimal ValorLiquidado,
    decimal ValorPago);

/// <summary>
/// Consulta de leitura do extrato do credor — consolida os empenhos (com liquidação/pagamento acumulados)
/// de um credor identificado pelo documento normalizado. Lê dos agregados de execução da despesa.
/// </summary>
public interface ICredorExtratoConsulta
{
    /// <summary>Lista os empenhos do credor (por documento), opcionalmente filtrando por exercício.</summary>
    /// <param name="documento">Documento (CPF/CNPJ) normalizado do credor.</param>
    /// <param name="exercicio">Exercício a filtrar; <c>null</c> = todos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Itens do extrato.</returns>
    Task<IReadOnlyList<CredorExtratoItem>> ListarEmpenhosDoCredorAsync(
        string documento,
        int? exercicio,
        CancellationToken cancellationToken);
}
