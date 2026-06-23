using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.PortalPublico;

/// <summary>
/// Calendario de dias uteis PADRAO: pula fins de semana e os feriados NACIONAIS fixos (Lei 662/1949 +
/// Lei 6.802/1980). E o default parametrizavel — a fonte de feriados municipais/estaduais e moveis
/// (Carnaval/Corpus Christi, variaveis por ente) entra como refino sem alterar o agregado (o calculo do
/// prazo vive no dominio via <see cref="ICalendarioDiasUteis"/>; a fonte dos feriados, aqui).
/// Deterministico (sem relogio): mesmo par (inicio, dias) ⇒ mesma data.
/// // TODO(parametrizar-feriados): carregar feriados municipais/estaduais + moveis por tenant/exercicio
/// (mesma filosofia de ICalendarioFiscal/IParametroMinimoProvider). O default cobre os nacionais fixos.
/// </summary>
public sealed class CalendarioDiasUteisPadrao : ICalendarioDiasUteis
{
    // Feriados nacionais de data FIXA (mes, dia). Os moveis dependem da Pascoa — refino futuro.
    private static readonly (int Mes, int Dia)[] FeriadosNacionaisFixos =
    [
        (1, 1),   // Confraternizacao Universal
        (4, 21),  // Tiradentes
        (5, 1),   // Dia do Trabalho
        (9, 7),   // Independencia
        (10, 12), // Nossa Senhora Aparecida
        (11, 2),  // Finados
        (11, 15), // Proclamacao da Republica
        (12, 25), // Natal
    ];

    /// <inheritdoc />
    public DateOnly SomarDiasUteis(DateOnly inicio, int diasUteis)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(diasUteis);

        var atual = inicio;
        var restantes = diasUteis;
        while (restantes > 0)
        {
            atual = atual.AddDays(1);
            if (EhDiaUtil(atual))
            {
                restantes--;
            }
        }

        return atual;
    }

    private static bool EhDiaUtil(DateOnly data)
    {
        if (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        foreach (var (mes, dia) in FeriadosNacionaisFixos)
        {
            if (data.Month == mes && data.Day == dia)
            {
                return false;
            }
        }

        return true;
    }
}
