using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Itbi;

/// <summary>Resultado da apuração do ITBI (memória de cálculo achatada para leitura/auditoria).</summary>
/// <param name="ImovelId">Imóvel apurado.</param>
/// <param name="Exercicio">Exercício do fato gerador.</param>
/// <param name="ValorVenalReferencia">Valor venal de referência (R$).</param>
/// <param name="ValorDeclarado">Valor declarado (R$).</param>
/// <param name="BaseCalculo">Base adotada — maior entre venal e declarado (R$).</param>
/// <param name="BaseFoiValorVenal">Verdadeiro se a base foi o valor venal.</param>
/// <param name="AliquotaPercentual">Alíquota aplicada (%).</param>
/// <param name="ImpostoBruto">Imposto bruto (R$).</param>
/// <param name="ValorIsencao">Isenção aplicada (R$).</param>
/// <param name="ImpostoDevido">ITBI devido (R$).</param>
public sealed record ResultadoItbi(
    Guid ImovelId,
    int Exercicio,
    decimal ValorVenalReferencia,
    decimal ValorDeclarado,
    decimal BaseCalculo,
    bool BaseFoiValorVenal,
    decimal AliquotaPercentual,
    decimal ImpostoBruto,
    decimal ValorIsencao,
    decimal ImpostoDevido);

/// <summary>
/// Apura (sem lançar) o ITBI de uma transmissão: base = maior entre valor venal de referência
/// (motor do Imóvel/PGV) e valor declarado. Útil para simulação/preview antes da guia. Ver M6-DESIGN §3.1.
/// </summary>
/// <param name="ImovelId">Imóvel transmitido.</param>
/// <param name="Exercicio">Exercício do fato gerador.</param>
/// <param name="ValorDeclarado">Valor declarado da transação (R$).</param>
/// <param name="PercentualIsencao">Percentual de isenção (lei municipal); padrão 0.</param>
/// <param name="UsarAliquotaSfh">Usa a alíquota reduzida do SFH; padrão falso.</param>
public sealed record CalcularItbiQuery(
    Guid ImovelId,
    int Exercicio,
    decimal ValorDeclarado,
    decimal PercentualIsencao = 0m,
    bool UsarAliquotaSfh = false) : IQuery<ResultadoItbi>;

/// <summary>Handler da apuração do ITBI.</summary>
public sealed class CalcularItbiHandler(
    IImovelRepository imoveis,
    IPlantaValoresRepository plantas,
    IAliquotaItbiRepository aliquotas)
    : IQueryHandler<CalcularItbiQuery, ResultadoItbi>
{
    /// <inheritdoc />
    public async Task<ResultadoItbi> Handle(CalcularItbiQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var memoria = await ApuradorItbi.ApurarAsync(
            new ImovelId(request.ImovelId),
            request.Exercicio,
            ValorMonetario.De(request.ValorDeclarado),
            new ParametrosItbi(request.PercentualIsencao, request.UsarAliquotaSfh),
            imoveis,
            plantas,
            aliquotas,
            cancellationToken).ConfigureAwait(false);

        return new ResultadoItbi(
            request.ImovelId,
            request.Exercicio,
            memoria.ValorVenalReferencia.Valor,
            memoria.ValorDeclarado.Valor,
            memoria.BaseCalculo.Valor,
            memoria.BaseFoiValorVenal,
            memoria.AliquotaPercentual,
            memoria.ImpostoBruto.Valor,
            memoria.ValorIsencao.Valor,
            memoria.ImpostoDevido.Valor);
    }
}
