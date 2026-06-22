using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

namespace Tensorroot.Gov.Modules.Tributos.Application.Iptu;

/// <summary>Resultado da apuração do IPTU (memória de cálculo achatada para leitura/auditoria).</summary>
/// <param name="ImovelId">Imóvel apurado.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
/// <param name="ValorVenal">Valor venal apurado (R$).</param>
/// <param name="ValorTerreno">Parcela do terreno (R$).</param>
/// <param name="ValorConstrucao">Parcela da construção (R$).</param>
/// <param name="AliquotaPercentual">Alíquota aplicada (%).</param>
/// <param name="ImpostoBruto">Imposto bruto (R$).</param>
/// <param name="ValorIsencao">Isenção aplicada (R$).</param>
/// <param name="ValorDesconto">Desconto aplicado (R$).</param>
/// <param name="ImpostoDevido">IPTU devido (R$).</param>
public sealed record ResultadoIptu(
    Guid ImovelId,
    int Exercicio,
    decimal ValorVenal,
    decimal ValorTerreno,
    decimal ValorConstrucao,
    decimal AliquotaPercentual,
    decimal ImpostoBruto,
    decimal ValorIsencao,
    decimal ValorDesconto,
    decimal ImpostoDevido);

/// <summary>
/// Apura (sem lançar) o IPTU de um imóvel num exercício, usando a PGV e a tabela de alíquotas
/// vigentes. Útil para simulação/preview antes do lançamento de ofício.
/// </summary>
/// <param name="ImovelId">Imóvel a apurar.</param>
/// <param name="Exercicio">Exercício fiscal.</param>
/// <param name="PercentualIsencao">Percentual de isenção (lei municipal); padrão 0.</param>
/// <param name="PercentualDesconto">Percentual de desconto (lei municipal); padrão 0.</param>
public sealed record CalcularIptuQuery(
    Guid ImovelId,
    int Exercicio,
    decimal PercentualIsencao = 0m,
    decimal PercentualDesconto = 0m) : IQuery<ResultadoIptu>;

/// <summary>Handler da apuração do IPTU.</summary>
public sealed class CalcularIptuHandler(
    IImovelRepository imoveis,
    IPlantaValoresRepository plantas,
    ITabelaAliquotaIptuRepository tabelas)
    : IQueryHandler<CalcularIptuQuery, ResultadoIptu>
{
    /// <inheritdoc />
    public async Task<ResultadoIptu> Handle(CalcularIptuQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var memoria = await ApuradorIptu.ApurarAsync(
            new ImovelId(request.ImovelId),
            request.Exercicio,
            new ParametrosIsencaoIptu(request.PercentualIsencao, request.PercentualDesconto),
            imoveis,
            plantas,
            tabelas,
            cancellationToken).ConfigureAwait(false);

        var vv = memoria.ValorVenal;
        return new ResultadoIptu(
            request.ImovelId,
            request.Exercicio,
            vv.ValorVenal.Valor,
            vv.ValorTerreno,
            vv.ValorConstrucao,
            memoria.AliquotaPercentual,
            memoria.ImpostoBruto.Valor,
            memoria.ValorIsencao.Valor,
            memoria.ValorDesconto.Valor,
            memoria.ImpostoDevido.Valor);
    }
}
