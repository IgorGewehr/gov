using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Tempo;

/// <summary>
/// Implementacao do algoritmo de DIAS UTEIS: pula fins de semana e os feriados do tenant fornecidos pelo
/// <see cref="IFeriadosTenantProvider"/> (nacionais fixos ∪ moveis ∪ municipais ∪ facultativos adotados).
/// DETERMINISTICA: nao le relogio — toda contagem recebe a data base. Quando uma contagem cruza a fronteira
/// de ano, une os feriados dos anos envolvidos. Substitui a antiga <c>CalendarioDiasUteisPadrao</c>
/// (que cobria so fins de semana + nacionais fixos, com o <c>TODO(parametrizar-feriados)</c>).
/// </summary>
public sealed class CalendarioDiasUteis(IFeriadosTenantProvider feriados) : ICalendarioDiasUteis
{
    private readonly IFeriadosTenantProvider _feriados = feriados ?? throw new ArgumentNullException(nameof(feriados));

    /// <inheritdoc />
    public DateOnly AdicionarDiasUteis(DateOnly inicio, int diasUteis)
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

    /// <inheritdoc />
    public bool EhDiaUtil(DateOnly data)
    {
        if (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        return !_feriados.FeriadosDoAno(data.Year).Contains(data);
    }

    /// <inheritdoc />
    public DateOnly ProximoDiaUtil(DateOnly data)
    {
        var atual = data;
        while (!EhDiaUtil(atual))
        {
            atual = atual.AddDays(1);
        }

        return atual;
    }

    /// <inheritdoc />
    public int DiasUteisEntre(DateOnly a, DateOnly b)
    {
        if (b < a)
        {
            return -DiasUteisEntre(b, a);
        }

        // Exclusivo no inicio, inclusivo no fim (dias uteis "decorridos").
        var contador = 0;
        var atual = a;
        while (atual < b)
        {
            atual = atual.AddDays(1);
            if (EhDiaUtil(atual))
            {
                contador++;
            }
        }

        return contador;
    }
}
