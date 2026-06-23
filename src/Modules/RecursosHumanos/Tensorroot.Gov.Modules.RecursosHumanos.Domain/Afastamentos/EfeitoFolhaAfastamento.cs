namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;

/// <summary>
/// Efeito DETERMINISTICO de um (ou mais) afastamento(s) vigente(s) sobre o PROVENTO-BASE de um servidor
/// numa competencia. Calculado a partir da <see cref="RegraAfastamento"/> do tipo (parametrizada por
/// tenant) e dos dias afastados na competencia. Objeto de valor imutavel de transporte (dominio puro):
/// o gancho da folha aplica <see cref="AplicarAoProvento"/> ao valor do vencimento ANTES de lancar a
/// verba, sem alterar o <c>MotorDeCalculoFolha</c> (que continua puro). Nunca reduz abaixo de zero.
/// </summary>
/// <param name="DiasAfastadosNaCompetencia">Dias do mes em que o servidor esteve afastado (0..DiasNoMes).</param>
/// <param name="DiasNoMes">Dias totais da competencia (base da proporcionalidade).</param>
/// <param name="DiasPagosPeloEntePelaRegra">Dias do afastamento que o ENTE ainda paga integral (ex.: doenca 15d).</param>
/// <param name="SuspendeAposDiasDoEnte">Se, esgotados os dias pagos pelo ente, o provento e suspenso (INSS/sem vencimento).</param>
/// <param name="PercentualRemuneracao">Percentual (0..100) do provento mantido pelo ente nos dias remunerados.</param>
public sealed record EfeitoFolhaAfastamento(
    int DiasAfastadosNaCompetencia,
    int DiasNoMes,
    int DiasPagosPeloEntePelaRegra,
    bool SuspendeAposDiasDoEnte,
    decimal PercentualRemuneracao)
{
    /// <summary>Efeito NEUTRO (sem afastamento vigente): nao altera o provento.</summary>
    /// <param name="diasNoMes">Dias da competencia.</param>
    /// <returns>Efeito que mantem o provento integral.</returns>
    public static EfeitoFolhaAfastamento Neutro(int diasNoMes)
        => new(0, diasNoMes, 0, SuspendeAposDiasDoEnte: false, PercentualRemuneracao: 100m);

    /// <summary>Indica se o afastamento altera o provento-base na competencia.</summary>
    public bool AfetaFolha => DiasAfastadosNaCompetencia > 0
        && (SuspendeAposDiasDoEnte || PercentualRemuneracao < 100m);

    /// <summary>
    /// Aplica o efeito ao provento-base mensal (vencimento), de forma proporcional aos dias:
    /// os dias trabalhados (mes - afastado) sao pagos integralmente; dos dias afastados, os
    /// <see cref="DiasPagosPeloEntePelaRegra"/> iniciais (no maximo) sao pagos pelo ente segundo o
    /// <see cref="PercentualRemuneracao"/>; o restante e suspenso quando <see cref="SuspendeAposDiasDoEnte"/>
    /// (beneficio do INSS / sem vencimento) ou mantido no percentual nos demais casos. Deterministico.
    /// </summary>
    /// <param name="proventoBaseMensal">Valor cheio do provento-base (vencimento) do mes.</param>
    /// <returns>Provento ajustado pelo afastamento (2 casas), nunca negativo.</returns>
    public decimal AplicarAoProvento(decimal proventoBaseMensal)
    {
        if (proventoBaseMensal <= 0m || DiasNoMes <= 0 || DiasAfastadosNaCompetencia <= 0)
        {
            return decimal.Round(Math.Max(0m, proventoBaseMensal), 2, MidpointRounding.AwayFromZero);
        }

        var diasAfastados = Math.Min(DiasAfastadosNaCompetencia, DiasNoMes);
        var diasTrabalhados = DiasNoMes - diasAfastados;
        var valorDia = proventoBaseMensal / DiasNoMes;
        var fracaoPercentual = PercentualRemuneracao / 100m;

        // Dias afastados que o ente ainda remunera (limitado pela regra do tipo e pelos dias afastados).
        var diasRemuneradosPeloEnte = Math.Min(diasAfastados, DiasPagosPeloEntePelaRegra);
        // Dias afastados restantes: suspensos (INSS/sem vencimento) ou no percentual da regra.
        var diasRestantes = diasAfastados - diasRemuneradosPeloEnte;

        var valorTrabalhado = valorDia * diasTrabalhados;
        var valorEnte = valorDia * diasRemuneradosPeloEnte * fracaoPercentual;
        var valorRestante = SuspendeAposDiasDoEnte
            ? 0m
            : valorDia * diasRestantes * fracaoPercentual;

        var total = valorTrabalhado + valorEnte + valorRestante;
        return decimal.Round(total < 0m ? 0m : total, 2, MidpointRounding.AwayFromZero);
    }
}
