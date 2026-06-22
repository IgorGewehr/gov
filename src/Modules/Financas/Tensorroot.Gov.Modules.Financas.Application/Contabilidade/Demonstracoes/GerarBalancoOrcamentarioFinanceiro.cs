using Tensorroot.Gov.BuildingBlocks.Application.Messaging;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;

/// <summary>Gera o Balanço Orçamentário (Anexo 12) de uma competência (RBAC <c>financas.ver</c>).</summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Mes">Mês (1-12).</param>
public sealed record GerarBalancoOrcamentarioQuery(int Exercicio, int Mes) : IQuery<BalancoOrcamentarioDto>;

/// <summary>Gera o Balanço Financeiro (Anexo 13) de uma competência (RBAC <c>financas.ver</c>).</summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Mes">Mês (1-12).</param>
public sealed record GerarBalancoFinanceiroQuery(int Exercicio, int Mes) : IQuery<BalancoFinanceiroDto>;

/// <summary>
/// Handler do Balanço Orçamentário: receita realizada (classe 6.2.1.2) × despesa empenhada (classe
/// 6.2.2.1.3); o resultado orçamentário (superávit/déficit) é a diferença. Quadros de Restos a Pagar: M3.x.
/// </summary>
public sealed class GerarBalancoOrcamentarioHandler(DemonstrativoContexto contexto)
    : IQueryHandler<GerarBalancoOrcamentarioQuery, BalancoOrcamentarioDto>
{
    /// <inheritdoc />
    public async Task<BalancoOrcamentarioDto> Handle(
        GerarBalancoOrcamentarioQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agregador = await contexto.CarregarAsync(request.Exercicio, request.Mes, cancellationToken).ConfigureAwait(false);

        var mapaReceitas = MapaDemonstrativosCatalogo.Quadro(request.Exercicio, Demonstrativo.BalancoOrcamentario, "Receitas");
        var mapaDespesas = MapaDemonstrativosCatalogo.Quadro(request.Exercicio, Demonstrativo.BalancoOrcamentario, "Despesas");

        var linhasReceita = mapaReceitas
            .Select(m => new LinhaDemonstrativoDto(m.Linha, [agregador.Somar(m)]))
            .ToList();
        var linhasDespesa = mapaDespesas
            .Select(m => new LinhaDemonstrativoDto(m.Linha, [agregador.Somar(m)]))
            .ToList();

        var totalReceita = linhasReceita.Sum(l => l.Valores[0]);
        var totalDespesa = linhasDespesa.Sum(l => l.Valores[0]);

        var receitas = new QuadroDemonstrativoDto("Receitas", ["Receitas Realizadas"], linhasReceita);
        var despesas = new QuadroDemonstrativoDto("Despesas", ["Despesas Empenhadas"], linhasDespesa);

        return new BalancoOrcamentarioDto(
            request.Exercicio,
            request.Mes,
            receitas,
            despesas,
            totalReceita,
            totalDespesa,
            totalReceita - totalDespesa);
    }
}

/// <summary>
/// Handler do Balanço Financeiro: ingressos (receita orçamentária + saldo de abertura) × dispêndios
/// (despesa orçamentária). Segregação por destinação de recurso (FR) e extraorçamentários: M3.x.
/// </summary>
public sealed class GerarBalancoFinanceiroHandler(DemonstrativoContexto contexto)
    : IQueryHandler<GerarBalancoFinanceiroQuery, BalancoFinanceiroDto>
{
    /// <inheritdoc />
    public async Task<BalancoFinanceiroDto> Handle(
        GerarBalancoFinanceiroQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agregador = await contexto.CarregarAsync(request.Exercicio, request.Mes, cancellationToken).ConfigureAwait(false);

        var mapaIngressos = MapaDemonstrativosCatalogo.Quadro(request.Exercicio, Demonstrativo.BalancoFinanceiro, "Ingressos");
        var mapaDispendios = MapaDemonstrativosCatalogo.Quadro(request.Exercicio, Demonstrativo.BalancoFinanceiro, "Dispendios");

        var ingressos = mapaIngressos
            .Select(m => new LinhaDemonstrativoDto(m.Linha, [agregador.Somar(m)]))
            .ToList();
        var dispendios = mapaDispendios
            .Select(m => new LinhaDemonstrativoDto(m.Linha, [agregador.Somar(m)]))
            .ToList();

        return new BalancoFinanceiroDto(
            request.Exercicio,
            request.Mes,
            ingressos,
            dispendios,
            ingressos.Sum(l => l.Valores[0]),
            dispendios.Sum(l => l.Valores[0]));
    }
}
