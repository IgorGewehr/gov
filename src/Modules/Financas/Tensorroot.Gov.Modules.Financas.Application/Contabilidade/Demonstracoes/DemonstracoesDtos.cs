namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;

/// <summary>Linha genérica de um quadro de demonstrativo (rótulo + valores por coluna).</summary>
/// <param name="Linha">Rótulo da linha.</param>
/// <param name="Valores">Valores por coluna (na ordem das colunas do quadro).</param>
public sealed record LinhaDemonstrativoDto(string Linha, IReadOnlyList<decimal> Valores);

/// <summary>Quadro de um demonstrativo (título + colunas + linhas + totais).</summary>
/// <param name="Quadro">Título do quadro.</param>
/// <param name="Colunas">Rótulos das colunas.</param>
/// <param name="Linhas">Linhas do quadro.</param>
public sealed record QuadroDemonstrativoDto(
    string Quadro,
    IReadOnlyList<string> Colunas,
    IReadOnlyList<LinhaDemonstrativoDto> Linhas);

/// <summary>
/// Balanço Orçamentário (Anexo 12): quadros de Receitas e Despesas, com o resultado orçamentário
/// (superávit/déficit) como linha de equilíbrio. Quadros de Restos a Pagar ficam para M3.x.
/// </summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="Receitas">Quadro de receitas.</param>
/// <param name="Despesas">Quadro de despesas.</param>
/// <param name="TotalReceitaRealizada">Total da receita realizada.</param>
/// <param name="TotalDespesaEmpenhada">Total da despesa empenhada.</param>
/// <param name="ResultadoOrcamentario">Receita realizada − despesa empenhada (déficit se negativo).</param>
public sealed record BalancoOrcamentarioDto(
    int Exercicio,
    int Mes,
    QuadroDemonstrativoDto Receitas,
    QuadroDemonstrativoDto Despesas,
    decimal TotalReceitaRealizada,
    decimal TotalDespesaEmpenhada,
    decimal ResultadoOrcamentario);

/// <summary>
/// Balanço Financeiro (Anexo 13): quadro único Ingressos × Dispêndios. Saldo em espécie de
/// abertura/encerramento incluídos. Segregação por destinação de recurso (FR) fica para M3.x.
/// </summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="Ingressos">Linhas de ingressos.</param>
/// <param name="Dispendios">Linhas de dispêndios.</param>
/// <param name="TotalIngressos">Total de ingressos.</param>
/// <param name="TotalDispendios">Total de dispêndios.</param>
public sealed record BalancoFinanceiroDto(
    int Exercicio,
    int Mes,
    IReadOnlyList<LinhaDemonstrativoDto> Ingressos,
    IReadOnlyList<LinhaDemonstrativoDto> Dispendios,
    decimal TotalIngressos,
    decimal TotalDispendios);

/// <summary>
/// Balanço Patrimonial (Anexo 14): quadro principal (Ativo × Passivo+PL), quadro de financeiros/
/// permanentes e superávit/déficit financeiro. Contas de compensação (7/8) e quebra por FR: M3.x.
/// </summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="Ativo">Linhas do ativo.</param>
/// <param name="PassivoPatrimonioLiquido">Linhas do passivo e PL.</param>
/// <param name="TotalAtivo">Total do ativo (Σ classe 1).</param>
/// <param name="TotalPassivoPl">Total do passivo + PL (Σ classe 2).</param>
/// <param name="AtivoFinanceiro">Ativo financeiro (classe 1, indicador F).</param>
/// <param name="PassivoFinanceiro">Passivo financeiro (classe 2, indicador F).</param>
/// <param name="SuperavitFinanceiro">Ativo financeiro − passivo financeiro (art. 105 Lei 4.320).</param>
public sealed record BalancoPatrimonialDto(
    int Exercicio,
    int Mes,
    IReadOnlyList<LinhaDemonstrativoDto> Ativo,
    IReadOnlyList<LinhaDemonstrativoDto> PassivoPatrimonioLiquido,
    decimal TotalAtivo,
    decimal TotalPassivoPl,
    decimal AtivoFinanceiro,
    decimal PassivoFinanceiro,
    decimal SuperavitFinanceiro);

/// <summary>
/// Demonstração das Variações Patrimoniais (Anexo 15): VPA (classe 4) − VPD (classe 3) = resultado
/// patrimonial do período.
/// </summary>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="VariacoesAumentativas">Linhas de VPA (classe 4).</param>
/// <param name="VariacoesDiminutivas">Linhas de VPD (classe 3).</param>
/// <param name="TotalVpa">Total das VPA.</param>
/// <param name="TotalVpd">Total das VPD.</param>
/// <param name="ResultadoPatrimonial">VPA − VPD.</param>
public sealed record DemonstracaoVariacoesPatrimoniaisDto(
    int Exercicio,
    int Mes,
    IReadOnlyList<LinhaDemonstrativoDto> VariacoesAumentativas,
    IReadOnlyList<LinhaDemonstrativoDto> VariacoesDiminutivas,
    decimal TotalVpa,
    decimal TotalVpd,
    decimal ResultadoPatrimonial);
