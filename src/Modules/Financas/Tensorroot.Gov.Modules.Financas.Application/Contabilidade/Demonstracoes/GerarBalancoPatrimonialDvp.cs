using Tensorroot.Gov.BuildingBlocks.Application.Messaging;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;

/// <summary>Gera o Balanço Patrimonial (Anexo 14) de uma competência (RBAC <c>financas.ver</c>).</summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Mes">Mês (1-12).</param>
public sealed record GerarBalancoPatrimonialQuery(int Exercicio, int Mes) : IQuery<BalancoPatrimonialDto>;

/// <summary>Gera a Demonstração das Variações Patrimoniais (Anexo 15) (RBAC <c>financas.ver</c>).</summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Mes">Mês (1-12).</param>
public sealed record GerarDvpQuery(int Exercicio, int Mes) : IQuery<DemonstracaoVariacoesPatrimoniaisDto>;

/// <summary>
/// Handler do Balanço Patrimonial: quadro principal (Ativo classe 1 × Passivo+PL classe 2) e o
/// superávit/déficit financeiro (Ativo Financeiro − Passivo Financeiro, art. 105 Lei 4.320), segregando
/// por indicador F/P da conta. Contas de compensação (7/8) e quebra por FR: M3.x.
/// </summary>
public sealed class GerarBalancoPatrimonialHandler(DemonstrativoContexto contexto)
    : IQueryHandler<GerarBalancoPatrimonialQuery, BalancoPatrimonialDto>
{
    /// <inheritdoc />
    public async Task<BalancoPatrimonialDto> Handle(
        GerarBalancoPatrimonialQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agregador = await contexto.CarregarAsync(request.Exercicio, request.Mes, cancellationToken).ConfigureAwait(false);

        var mapaAtivo = MapaDemonstrativosCatalogo.Quadro(request.Exercicio, Demonstrativo.BalancoPatrimonial, "Ativo");
        var mapaPassivo = MapaDemonstrativosCatalogo.Quadro(request.Exercicio, Demonstrativo.BalancoPatrimonial, "Passivo");

        var ativo = mapaAtivo
            .Select(m => new LinhaDemonstrativoDto(m.Linha, [agregador.Somar(m)]))
            .ToList();
        var passivoPl = mapaPassivo
            .Select(m => new LinhaDemonstrativoDto(m.Linha, [agregador.Somar(m)]))
            .ToList();

        // Totais por classe (limpos, sem depender da quebra de linhas do mapa).
        var totalAtivo = agregador.Somar("1");
        var totalPassivoPl = agregador.Somar("2");

        var ativoFinanceiro = agregador.Somar("1", FiltroSuperavit.Financeiro);
        var passivoFinanceiro = agregador.Somar("2", FiltroSuperavit.Financeiro);

        return new BalancoPatrimonialDto(
            request.Exercicio,
            request.Mes,
            ativo,
            passivoPl,
            totalAtivo,
            totalPassivoPl,
            ativoFinanceiro,
            passivoFinanceiro,
            ativoFinanceiro - passivoFinanceiro);
    }
}

/// <summary>
/// Handler da DVP: VPA (classe 4) − VPD (classe 3) = resultado patrimonial. As linhas "Outras" recebem o
/// resíduo da classe não capturado pelas linhas detalhadas (evita dupla contagem do total da classe).
/// </summary>
public sealed class GerarDvpHandler(DemonstrativoContexto contexto)
    : IQueryHandler<GerarDvpQuery, DemonstracaoVariacoesPatrimoniaisDto>
{
    private const string LinhaResidual = "Outras";

    /// <inheritdoc />
    public async Task<DemonstracaoVariacoesPatrimoniaisDto> Handle(
        GerarDvpQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var agregador = await contexto.CarregarAsync(request.Exercicio, request.Mes, cancellationToken).ConfigureAwait(false);

        var totalVpa = agregador.Somar("4");
        var totalVpd = agregador.Somar("3");

        var vpa = MontarComResiduo(
            MapaDemonstrativosCatalogo.Quadro(request.Exercicio, Demonstrativo.DemonstracaoVariacoesPatrimoniais, "VPA"),
            agregador,
            totalVpa);
        var vpd = MontarComResiduo(
            MapaDemonstrativosCatalogo.Quadro(request.Exercicio, Demonstrativo.DemonstracaoVariacoesPatrimoniais, "VPD"),
            agregador,
            totalVpd);

        return new DemonstracaoVariacoesPatrimoniaisDto(
            request.Exercicio,
            request.Mes,
            vpa,
            vpd,
            totalVpa,
            totalVpd,
            totalVpa - totalVpd);
    }

    // As linhas detalhadas somam seus prefixos; a linha "Outras" recebe (total da classe − detalhadas).
    private static List<LinhaDemonstrativoDto> MontarComResiduo(
        IReadOnlyList<MapaLinhaDemonstrativo> mapa,
        AgregadorDemonstrativo agregador,
        decimal totalClasse)
    {
        var detalhadas = mapa
            .Where(m => !m.Linha.StartsWith(LinhaResidual, StringComparison.Ordinal))
            .Select(m => new LinhaDemonstrativoDto(m.Linha, [agregador.Somar(m)]))
            .ToList();

        var somaDetalhadas = detalhadas.Sum(l => l.Valores[0]);
        var rotuloResidual = mapa
            .FirstOrDefault(m => m.Linha.StartsWith(LinhaResidual, StringComparison.Ordinal))?.Linha
            ?? "Outras";

        detalhadas.Add(new LinhaDemonstrativoDto(rotuloResidual, [totalClasse - somaDetalhadas]));
        return detalhadas;
    }
}
