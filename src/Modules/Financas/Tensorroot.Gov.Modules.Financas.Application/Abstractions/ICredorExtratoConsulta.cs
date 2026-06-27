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

/// <summary>Retenções/consignações de um credor agrupadas por natureza, para o extrato.</summary>
/// <param name="Natureza">Natureza da retenção (IRRF-PJ, INSS, ISS, CSLL/COFINS/PIS, caução…).</param>
/// <param name="ValorRetido">Soma do valor retido na natureza (todas as liquidações do credor).</param>
/// <param name="ValorRecolhido">Soma já recolhida a terceiro (retenções baixadas por guia).</param>
/// <param name="ValorAReter">Saldo a recolher (retido − recolhido) — passivo extra-orçamentário em aberto.</param>
public sealed record CredorRetencaoResumo(
    string Natureza,
    decimal ValorRetido,
    decimal ValorRecolhido,
    decimal ValorAReter);

/// <summary>
/// Consulta de leitura do extrato do credor — consolida os empenhos (com liquidação/pagamento acumulados)
/// e as retenções/consignações de um credor identificado pelo documento normalizado. Lê dos agregados de
/// execução da despesa (Empenho/Liquidacao) sem quebrar o isolamento de tenant (Global Query Filter).
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

    /// <summary>
    /// Consolida as retenções do credor (via Liquidacao→Empenho→Credor.Documento) agrupadas por natureza,
    /// opcionalmente filtrando pelo exercício do empenho.
    /// </summary>
    /// <param name="documento">Documento (CPF/CNPJ) normalizado do credor.</param>
    /// <param name="exercicio">Exercício a filtrar; <c>null</c> = todos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Retenções por natureza.</returns>
    Task<IReadOnlyList<CredorRetencaoResumo>> ListarRetencoesDoCredorAsync(
        string documento,
        int? exercicio,
        CancellationToken cancellationToken);
}
