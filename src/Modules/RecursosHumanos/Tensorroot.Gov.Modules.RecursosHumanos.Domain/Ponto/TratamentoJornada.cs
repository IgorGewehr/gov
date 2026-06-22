namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>
/// Marcacao reduzida (instante + sentido) consumida pelo tratamento de jornada. Permite calcular sem
/// acoplar o servico de dominio ao agregado <see cref="MarcacaoPonto"/> (testabilidade/pureza).
/// </summary>
/// <param name="DataHora">Instante exato da batida.</param>
/// <param name="Sentido">Entrada ou saida.</param>
public readonly record struct InstanteMarcacao(DateTimeOffset DataHora, SentidoMarcacao Sentido);

/// <summary>
/// Resultado da apuracao de UM dia: minutos trabalhados (a partir do pareamento entrada/saida),
/// minutos devidos (jornada), extras e falta/atraso (apos tolerancia). Determinístico.
/// </summary>
/// <param name="Dia">Dia apurado.</param>
/// <param name="MinutosTrabalhados">Minutos efetivamente trabalhados (soma dos periodos pareados).</param>
/// <param name="MinutosDevidos">Carga diaria contratada (jornada).</param>
/// <param name="MinutosExtras">Excedente apos a tolerancia (>= 0).</param>
/// <param name="MinutosFalta">Deficit apos a tolerancia (>= 0).</param>
/// <param name="MarcacaoImpar">Verdadeiro quando ha numero impar de marcacoes no dia (inconsistencia a tratar no espelho).</param>
public readonly record struct ApuracaoDia(
    DateOnly Dia,
    int MinutosTrabalhados,
    int MinutosDevidos,
    int MinutosExtras,
    int MinutosFalta,
    bool MarcacaoImpar);

/// <summary>
/// Totalizacao da apuracao de uma competencia: somatorios de trabalhado/devido/extra/falta e o saldo
/// do periodo (extras menos faltas), insumo do banco de horas.
/// </summary>
/// <param name="MinutosTrabalhados">Total trabalhado no periodo.</param>
/// <param name="MinutosDevidos">Total devido no periodo.</param>
/// <param name="MinutosExtras">Total de extras no periodo.</param>
/// <param name="MinutosFalta">Total de falta/atraso no periodo.</param>
/// <param name="Dias">Apuracao detalhada por dia.</param>
public sealed record ResultadoApuracaoCompetencia(
    int MinutosTrabalhados,
    int MinutosDevidos,
    int MinutosExtras,
    int MinutosFalta,
    IReadOnlyList<ApuracaoDia> Dias)
{
    /// <summary>Saldo do periodo (extras menos faltas) — credita/debita o banco de horas.</summary>
    public int SaldoBancoHorasMinutos => MinutosExtras - MinutosFalta;
}

/// <summary>
/// Programa de Tratamento de Registro de Ponto (PTRP — Portaria MTP 671/2021): servico de dominio PURO
/// (sem I/O) que TRATA as marcacoes brutas do AFD para produzir o insumo do AEJ e do espelho de ponto,
/// SEM ALTERAR o AFD (imutavel). Pareia entradas/saidas por dia, soma o trabalhado, compara com a
/// jornada e apura extras/faltas respeitando a tolerancia diaria.
/// // TODO(validar-oficial): regras finas (arredondamentos, intervalo nao deduzido, adicional noturno,
/// DSR) conforme Anexo VI (AEJ) e CLT art. 58/59 — aqui aplicamos o CONCEITO base.
/// </summary>
public static class TratamentoJornada
{
    /// <summary>
    /// Apura uma competencia inteira para um servidor: agrupa as marcacoes por dia, trata cada dia e
    /// totaliza. As marcacoes nao precisam vir ordenadas; o tratamento ordena por instante.
    /// </summary>
    /// <param name="marcacoes">Marcacoes brutas do periodo (qualquer ordem).</param>
    /// <param name="jornada">Jornada vigente do servidor (carga/intervalo/tolerancia).</param>
    /// <returns>Resultado totalizado da competencia.</returns>
    /// <exception cref="ArgumentNullException">Se marcacoes ou jornada forem nulos.</exception>
    public static ResultadoApuracaoCompetencia Apurar(
        IReadOnlyCollection<InstanteMarcacao> marcacoes,
        JornadaTrabalho jornada)
    {
        ArgumentNullException.ThrowIfNull(marcacoes);
        ArgumentNullException.ThrowIfNull(jornada);

        var dias = new List<ApuracaoDia>();
        foreach (var grupo in marcacoes
            .GroupBy(m => DateOnly.FromDateTime(m.DataHora.LocalDateTime.Date))
            .OrderBy(g => g.Key))
        {
            dias.Add(ApurarDia(grupo.Key, grupo, jornada));
        }

        return new ResultadoApuracaoCompetencia(
            MinutosTrabalhados: dias.Sum(d => d.MinutosTrabalhados),
            MinutosDevidos: dias.Sum(d => d.MinutosDevidos),
            MinutosExtras: dias.Sum(d => d.MinutosExtras),
            MinutosFalta: dias.Sum(d => d.MinutosFalta),
            Dias: dias);
    }

    private static ApuracaoDia ApurarDia(DateOnly dia, IEnumerable<InstanteMarcacao> doDia, JornadaTrabalho jornada)
    {
        var ordenadas = doDia.OrderBy(m => m.DataHora).ToList();
        var trabalhados = 0;
        var impar = false;

        // Pareia entrada->saida na ordem cronologica. Um sentido fora de ordem (ex.: duas entradas
        // seguidas) e ignorado para o par corrente e marcado como inconsistencia (espelho/AEJ).
        InstanteMarcacao? entradaAberta = null;
        foreach (var marcacao in ordenadas)
        {
            if (marcacao.Sentido == SentidoMarcacao.Entrada)
            {
                if (entradaAberta is not null)
                {
                    impar = true; // entrada sem saida anterior pareada
                }

                entradaAberta = marcacao;
            }
            else if (entradaAberta is { } abertura)
            {
                var minutos = (int)Math.Round((marcacao.DataHora - abertura.DataHora).TotalMinutes, MidpointRounding.AwayFromZero);
                if (minutos > 0)
                {
                    trabalhados += minutos;
                }

                entradaAberta = null;
            }
            else
            {
                impar = true; // saida sem entrada
            }
        }

        if (entradaAberta is not null)
        {
            impar = true; // entrada sem saida ao fim do dia
        }

        // Tolerancia diaria (CLT art. 58 §1): nem extra nem falta dentro do limite.
        var devidos = jornada.CargaDiariaMinutos;
        var diferenca = trabalhados - devidos;
        var extras = 0;
        var falta = 0;
        if (diferenca > jornada.ToleranciaMinutos)
        {
            extras = diferenca;
        }
        else if (-diferenca > jornada.ToleranciaMinutos)
        {
            falta = -diferenca;
        }

        return new ApuracaoDia(dia, trabalhados, devidos, extras, falta, impar);
    }
}
