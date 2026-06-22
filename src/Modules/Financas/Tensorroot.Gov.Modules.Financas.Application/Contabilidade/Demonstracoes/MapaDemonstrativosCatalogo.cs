namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;

/// <summary>
/// Catálogo parametrizável (por exercício) do mapa conta→linha dos demonstrativos DCASP núcleo (BO, BF,
/// BP, DVP). É o ponto único onde os prefixos PCASP de cada linha vivem — o cálculo apenas aplica este
/// mapa sobre o balancete (DCASP §9). Modelado fiel ao conceito dos Anexos 12–15; os rótulos/prefixos
/// linha-a-linha exatos ficam [validar-oficial] (MCASP 11ª ed. Parte V / Anexos da Lei 4.320).
/// </summary>
public static class MapaDemonstrativosCatalogo
{
    /// <summary>Retorna o mapa de linhas vigente para o exercício (núcleo M3).</summary>
    /// <param name="exercicio">Exercício de referência.</param>
    /// <returns>Linhas mapeadas dos quatro demonstrativos.</returns>
    public static IReadOnlyList<MapaLinhaDemonstrativo> Para(int exercicio) =>
    [
        // ===== Balanço Orçamentário (classes 5 e 6) =====
        // Receitas: previsão (classe 5.2.1) e realizada (classe 6.2.1.2).
        new(exercicio, Demonstrativo.BalancoOrcamentario, "Receitas", 1, "Receita Realizada", "6.2.1.2"),
        // Despesas: dotação (classe 5.2.2) e execução (classe 6.2.2.1.3).
        new(exercicio, Demonstrativo.BalancoOrcamentario, "Despesas", 1, "Despesa Empenhada", "6.2.2.1.3"),

        // ===== Balanço Financeiro =====
        new(exercicio, Demonstrativo.BalancoFinanceiro, "Ingressos", 1, "Receita Orcamentaria", "6.2.1.2"),
        new(exercicio, Demonstrativo.BalancoFinanceiro, "Ingressos", 2, "Saldo do Exercicio Anterior", "1.1.1"),
        new(exercicio, Demonstrativo.BalancoFinanceiro, "Dispendios", 1, "Despesa Orcamentaria", "6.2.2.1.3"),

        // ===== Balanço Patrimonial (classes 1 e 2) =====
        new(exercicio, Demonstrativo.BalancoPatrimonial, "Ativo", 1, "Ativo Circulante", "1.1"),
        new(exercicio, Demonstrativo.BalancoPatrimonial, "Ativo", 2, "Ativo Nao Circulante", "1.2"),
        new(exercicio, Demonstrativo.BalancoPatrimonial, "Passivo", 1, "Passivo Circulante", "2.1"),
        new(exercicio, Demonstrativo.BalancoPatrimonial, "Passivo", 2, "Passivo Nao Circulante", "2.2"),
        new(exercicio, Demonstrativo.BalancoPatrimonial, "Passivo", 3, "Patrimonio Liquido", "2.3"),

        // ===== Demonstração das Variações Patrimoniais (classes 3 e 4) =====
        new(exercicio, Demonstrativo.DemonstracaoVariacoesPatrimoniais, "VPA", 1, "Impostos, Taxas e Contrib.", "4.1"),
        new(exercicio, Demonstrativo.DemonstracaoVariacoesPatrimoniais, "VPA", 99, "Outras VPA", "4"),
        new(exercicio, Demonstrativo.DemonstracaoVariacoesPatrimoniais, "VPD", 1, "Uso de Bens e Servicos", "3.3"),
        new(exercicio, Demonstrativo.DemonstracaoVariacoesPatrimoniais, "VPD", 99, "Outras VPD", "3"),
    ];

    /// <summary>Filtra o catálogo por demonstrativo e quadro, ordenado.</summary>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="demonstrativo">Demonstrativo.</param>
    /// <param name="quadro">Quadro.</param>
    /// <returns>Linhas mapeadas do quadro, em ordem.</returns>
    public static IReadOnlyList<MapaLinhaDemonstrativo> Quadro(
        int exercicio,
        Demonstrativo demonstrativo,
        string quadro)
        => Para(exercicio)
            .Where(m => m.Demonstrativo == demonstrativo && string.Equals(m.Quadro, quadro, StringComparison.Ordinal))
            .OrderBy(m => m.Ordem)
            .ToList();
}
