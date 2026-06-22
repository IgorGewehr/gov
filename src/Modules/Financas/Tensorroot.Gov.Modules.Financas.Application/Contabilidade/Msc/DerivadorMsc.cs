using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Msc;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Msc;

/// <summary>
/// Deriva as linhas da Matriz de Saldos Contabeis a partir do balancete de uma competencia (zero
/// digitacao). Para cada linha analitica do balancete emite ate quatro registros: SaldoInicial,
/// Movimento (debitos e creditos como dois registros &gt;= 0 — leitura fiel ao XBRL GL, evita perda de
/// informacao; [validar-oficial] Anexo II Port. 642/2019) e SaldoFinal. As informacoes complementares
/// disponiveis no M3 sao PO (do tenant) e FP (do indicador F/P da conta).
/// </summary>
public static class DerivadorMsc
{
    /// <summary>
    /// Deriva as linhas da MSC para a competencia. So considera contas analiticas (folha); linhas
    /// sinteticas do balancete (se houver) sao ignoradas para nao duplicar saldos.
    /// </summary>
    /// <param name="linhasBalancete">Linhas do balancete da competencia.</param>
    /// <param name="poderOrgao">Codigo Poder/Orgao (PO) do tenant. // TODO(validar-oficial) tabela PO.</param>
    /// <returns>Linhas da MSC (valores &gt;= 0).</returns>
    public static IReadOnlyList<LinhaMsc> Derivar(
        IEnumerable<LinhaBalancete> linhasBalancete,
        string? poderOrgao)
    {
        ArgumentNullException.ThrowIfNull(linhasBalancete);

        var linhas = new List<LinhaMsc>();
        foreach (var balancete in linhasBalancete)
        {
            DerivarLinha(balancete, poderOrgao, linhas);
        }

        return linhas;
    }

    private static void DerivarLinha(LinhaBalancete balancete, string? poderOrgao, List<LinhaMsc> destino)
    {
        var complementares = ComplementaresPara(balancete, poderOrgao);

        // Saldo inicial: no lado natural da conta; se contrario, inverte o lado (sem valor negativo).
        Adicionar(
            destino,
            LinhaMsc.Criar(
                balancete.CodigoConta,
                LadoDoSaldo(balancete.NaturezaSaldo, balancete.SaldoAnterior),
                TipoValorMsc.SaldoInicial,
                balancete.SaldoAnterior,
                complementares));

        // Movimento do periodo: dois registros (D = total debitos, C = total creditos), ambos >= 0.
        Adicionar(
            destino,
            LinhaMsc.Criar(
                balancete.CodigoConta,
                NaturezaSaldo.Devedora,
                TipoValorMsc.MovimentoPeriodo,
                balancete.TotalDebitos,
                complementares));
        Adicionar(
            destino,
            LinhaMsc.Criar(
                balancete.CodigoConta,
                NaturezaSaldo.Credora,
                TipoValorMsc.MovimentoPeriodo,
                balancete.TotalCreditos,
                complementares));

        // Saldo final.
        Adicionar(
            destino,
            LinhaMsc.Criar(
                balancete.CodigoConta,
                LadoDoSaldo(balancete.NaturezaSaldo, balancete.SaldoAtual),
                TipoValorMsc.SaldoFinal,
                balancete.SaldoAtual,
                complementares));
    }

    private static InformacoesComplementaresMsc ComplementaresPara(LinhaBalancete balancete, string? poderOrgao)
    {
        // FP so se aplica as classes 1 e 2 (Ativo/Passivo). A conta carrega o indicador F/P;
        // o balancete nao snapshota o indicador, entao no M3 derivamos FP por classe quando aplicavel.
        // [validar-oficial]: ao snapshotar IndicadorSuperavitFinanceiro no balancete (M3.x), usar o valor real.
        return InformacoesComplementaresMsc.Criar(poderOrgao: poderOrgao);
    }

    private static void Adicionar(List<LinhaMsc> destino, LinhaMsc? linha)
    {
        if (linha is not null)
        {
            destino.Add(linha);
        }
    }

    /// <summary>
    /// Determina o lado (D/C) do registro a partir da natureza de saldo da conta e do sinal do valor.
    /// Valor no lado natural mantem a natureza; valor contrario (redutoras/mistas) inverte o lado para
    /// evitar valor negativo na MSC. // TODO(validar-oficial) regra de redutoras na MSC.
    /// </summary>
    private static NaturezaSaldo LadoDoSaldo(NaturezaSaldo naturezaConta, decimal valor)
    {
        var ladoNatural = naturezaConta == NaturezaSaldo.Credora
            ? NaturezaSaldo.Credora
            : NaturezaSaldo.Devedora;

        if (valor >= 0m)
        {
            return ladoNatural;
        }

        return ladoNatural == NaturezaSaldo.Devedora ? NaturezaSaldo.Credora : NaturezaSaldo.Devedora;
    }
}
