using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Itbi;

/// <summary>Resultado da apuração do ITBI (memória de cálculo achatada para leitura/auditoria).</summary>
/// <param name="ImovelId">Imóvel apurado.</param>
/// <param name="Exercicio">Exercício do fato gerador.</param>
/// <param name="ValorVenalReferencia">Valor venal de referência — parâmetro de triagem (R$).</param>
/// <param name="ValorDeclarado">Valor declarado (R$).</param>
/// <param name="BaseCalculo">Base adotada — valor declarado por padrão (R$).</param>
/// <param name="Origem">Origem da base: Declarada ou ArbitradaArt148.</param>
/// <param name="HaDivergenciaReferencia">Verdadeiro se a triagem sinalizou divergência relevante (não altera a base).</param>
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
    OrigemBaseCalculoItbi Origem,
    bool HaDivergenciaReferencia,
    decimal AliquotaPercentual,
    decimal ImpostoBruto,
    decimal ValorIsencao,
    decimal ImpostoDevido);

/// <summary>
/// Apura (sem lançar) o ITBI de uma transmissão: base = VALOR DECLARADO (Tema 1.113/STJ). O valor venal
/// de referência só dispara o alerta de triagem. Útil para simulação/preview antes da guia. Ver M6-DESIGN §3.1.
/// </summary>
/// <param name="ImovelId">Imóvel transmitido.</param>
/// <param name="Exercicio">Exercício do fato gerador.</param>
/// <param name="ValorDeclarado">Valor declarado da transação (R$).</param>
/// <param name="PercentualIsencao">Percentual de isenção (lei municipal); padrão 0.</param>
/// <param name="UsarAliquotaSfh">Usa a alíquota reduzida do SFH; padrão falso.</param>
/// <param name="MargemDivergenciaPercentual">Margem de tolerância da triagem em % (parametrizável por tenant); padrão 0.</param>
public sealed record CalcularItbiQuery(
    Guid ImovelId,
    int Exercicio,
    decimal ValorDeclarado,
    decimal PercentualIsencao = 0m,
    bool UsarAliquotaSfh = false,
    decimal MargemDivergenciaPercentual = 0m) : IQuery<ResultadoItbi>;

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
            request.MargemDivergenciaPercentual,
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
            memoria.Origem,
            memoria.HaDivergenciaReferencia,
            memoria.AliquotaPercentual,
            memoria.ImpostoBruto.Valor,
            memoria.ValorIsencao.Valor,
            memoria.ImpostoDevido.Valor);
    }
}
