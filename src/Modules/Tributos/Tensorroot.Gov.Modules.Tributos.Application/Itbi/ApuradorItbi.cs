using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Itbi;

/// <summary>
/// Orquestra a apuração do ITBI: carrega o imóvel + PGV vigente (reusa o motor de valor venal do
/// Imóvel/IPTU — part 1) para obter o valor venal de referência e a alíquota do ITBI vigente, e delega
/// ao motor de domínio (base = maior entre valor venal e valor declarado). Reutilizado pela query de
/// preview e pelo comando de lançamento — fonte única de verdade da apuração do ITBI. Ver M6-DESIGN §3.1.
/// </summary>
internal static class ApuradorItbi
{
    /// <summary>Apura o ITBI de uma transmissão, carregando os parâmetros vigentes.</summary>
    /// <param name="imovelId">Imóvel transmitido.</param>
    /// <param name="exercicio">Exercício do fato gerador.</param>
    /// <param name="valorDeclarado">Valor declarado da transação.</param>
    /// <param name="parametros">Isenção/uso de alíquota SFH (lei municipal).</param>
    /// <param name="imoveis">Repositório de imóveis.</param>
    /// <param name="plantas">Repositório de PGV.</param>
    /// <param name="aliquotas">Repositório de alíquotas do ITBI.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A memória de cálculo do ITBI.</returns>
    /// <exception cref="InvalidOperationException">Se o imóvel, a PGV ou a alíquota vigente não existirem.</exception>
    public static async Task<MemoriaItbi> ApurarAsync(
        ImovelId imovelId,
        int exercicio,
        ValorMonetario valorDeclarado,
        ParametrosItbi parametros,
        IImovelRepository imoveis,
        IPlantaValoresRepository plantas,
        IAliquotaItbiRepository aliquotas,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(valorDeclarado);
        ArgumentNullException.ThrowIfNull(parametros);

        var imovel = await imoveis.ObterPorIdAsync(imovelId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        if (!imovel.Ativo)
        {
            throw new InvalidOperationException("O imóvel está inativo no cadastro.");
        }

        var planta = await plantas.ObterVigenteAsync(exercicio, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Não há PGV vigente para o exercício {exercicio}.");

        var aliquota = await aliquotas.ObterVigenteAsync(exercicio, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Não há alíquota de ITBI vigente para o exercício {exercicio}.");

        // Reusa o motor do Imóvel/PGV (part 1) para o valor venal de referência.
        var memoriaVenal = CalculadoraValorVenal.Calcular(imovel, planta);

        return CalculadoraItbi.Calcular(
            memoriaVenal.ValorVenal,
            valorDeclarado,
            aliquota.AliquotaGeralPercentual,
            aliquota.AliquotaSfhFinanciadaPercentual,
            parametros);
    }
}
