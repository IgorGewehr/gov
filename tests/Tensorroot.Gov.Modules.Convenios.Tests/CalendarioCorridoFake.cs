using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Tests;

/// <summary>
/// Calendario de testes DETERMINISTICO que trata TODO dia como util (sem fins de semana/feriados): para
/// dias uteis, somar N dias uteis equivale a somar N dias corridos. Mantem as transicoes do agregado
/// testaveis e independentes de feriados do tenant — o foco aqui sao as INVARIANTES (prazos vencidos,
/// saneamento, bloqueio), nao o algoritmo de feriados (coberto no SharedKernel).
/// </summary>
internal sealed class CalendarioCorridoFake : ICalendarioDiasUteis
{
    public DateOnly AdicionarDiasUteis(DateOnly inicio, int diasUteis) => inicio.AddDays(diasUteis);

    public bool EhDiaUtil(DateOnly data) => true;

    public DateOnly ProximoDiaUtil(DateOnly data) => data;

    public int DiasUteisEntre(DateOnly a, DateOnly b) => b.DayNumber - a.DayNumber;
}
