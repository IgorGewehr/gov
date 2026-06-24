namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

/// <summary>Uma competência mensal de despesa com pessoal (entrada da janela móvel).</summary>
/// <param name="Ano">Ano (exercício) da competência.</param>
/// <param name="Mes">Mês da competência (1-12).</param>
/// <param name="Valor">Despesa de pessoal (base LRF) da competência.</param>
public readonly record struct CompetenciaPessoal(int Ano, int Mes, decimal Valor)
{
    /// <summary>Ordinal absoluto (ano*12 + mês) para ordenar/contar meses sem ambiguidade de virada de ano.</summary>
    public int Ordinal => (Ano * 12) + Mes;
}

/// <summary>Resultado da apuração da Despesa Total com Pessoal pela janela móvel de 12 meses.</summary>
/// <param name="Apurada">Verdadeiro quando havia pelo menos uma competência para definir o mês de referência.</param>
/// <param name="AnoReferencia">Ano do mês de referência (mês mais recente reportado).</param>
/// <param name="MesReferencia">Mês de referência (1-12; 0 quando não apurada).</param>
/// <param name="DespesaTotalPessoal">Soma das despesas dos 12 meses da janela (mês de referência + 11 anteriores).</param>
public readonly record struct DespesaPessoalDozeMeses(
    bool Apurada,
    int AnoReferencia,
    int MesReferencia,
    decimal DespesaTotalPessoal);

/// <summary>
/// Função de domínio PURA que apura a <b>Despesa Total com Pessoal (DTP)</b> da LRF (LC 101/2000 art. 18
/// §2º) pela <b>janela móvel de 12 meses</b>: o mês de referência (a competência mais recente reportada)
/// somada aos 11 meses imediatamente anteriores. Substitui o acumulado do exercício (que zera em janeiro
/// e subdimensiona o numerador de jan-nov, gerando indicador falso-verde). Determinística e sem relógio —
/// o mês de referência é derivado dos próprios dados (a competência mais recente), nunca do calendário do
/// servidor. A janela pode cruzar o exercício anterior; por isso recebe as competências de todos os
/// exercícios relevantes (o exercício consultado + o anterior).
/// </summary>
public static class DespesaPessoalDozeMesesCalculator
{
    /// <summary>Número de meses da janela móvel da DTP (LRF art. 18 §2º): mês de referência + 11 anteriores.</summary>
    public const int MesesDaJanela = 12;

    /// <summary>
    /// Apura a DTP pela janela móvel de 12 meses encerrada na competência mais recente. Meses ausentes
    /// dentro da janela contam como 0 (folha não reportada = sem despesa materializada para o KPI).
    /// </summary>
    /// <param name="competencias">Competências mensais disponíveis (qualquer ordem; exercícios variados).</param>
    /// <returns>A DTP de 12 meses e seu mês de referência; <c>Apurada=false</c> se não há competência.</returns>
    public static DespesaPessoalDozeMeses Apurar(IEnumerable<CompetenciaPessoal> competencias)
    {
        ArgumentNullException.ThrowIfNull(competencias);

        var lista = competencias.ToList();
        if (lista.Count == 0)
        {
            return new DespesaPessoalDozeMeses(false, 0, 0, 0m);
        }

        // Mês de referência = competência mais recente reportada (maior ordinal absoluto).
        var ordinalReferencia = lista.Max(c => c.Ordinal);
        var ordinalInicioJanela = ordinalReferencia - (MesesDaJanela - 1);

        var dtp = lista
            .Where(c => c.Ordinal >= ordinalInicioJanela && c.Ordinal <= ordinalReferencia)
            .Sum(c => c.Valor);

        var anoReferencia = (ordinalReferencia - 1) / 12;
        var mesReferencia = ordinalReferencia - (anoReferencia * 12);

        return new DespesaPessoalDozeMeses(true, anoReferencia, mesReferencia, dtp);
    }
}
