namespace Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

/// <summary>
/// Prazo legal/administrativo do processo (art. 66, Lei 9.784/1999). O calculo
/// <b>exclui o dia inicial e inclui o final</b>: a contagem comeca no dia seguinte
/// ao inicio e o vencimento ocorre apos transcorrer o numero de dias informado.
/// </summary>
public readonly record struct Prazo
{
    /// <summary>Cria um prazo a partir das datas de inicio e fim.</summary>
    /// <param name="inicio">Data de inicio (dia da autuacao/ato; excluida da contagem).</param>
    /// <param name="fim">Data final (incluida na contagem).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a data final for anterior a data de inicio.</exception>
    public Prazo(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio)
        {
            throw new ArgumentOutOfRangeException(nameof(fim), "Data final do prazo nao pode ser anterior ao inicio.");
        }

        Inicio = inicio;
        Fim = fim;
    }

    /// <summary>Data de inicio (excluida da contagem — art. 66).</summary>
    public DateOnly Inicio { get; }

    /// <summary>Data final (incluida na contagem — art. 66).</summary>
    public DateOnly Fim { get; }

    /// <summary>
    /// Cria um prazo a partir da data de inicio e de uma quantidade de dias, aplicando
    /// a regra do art. 66 (exclui o dia inicial e inclui o final): o vencimento e
    /// <paramref name="inicio"/> acrescido de <paramref name="dias"/> dias.
    /// </summary>
    /// <param name="inicio">Data de inicio (excluida da contagem).</param>
    /// <param name="dias">Quantidade de dias do prazo (maior que zero).</param>
    /// <returns>Prazo calculado conforme art. 66.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade de dias nao for positiva.</exception>
    public static Prazo APartirDe(DateOnly inicio, int dias)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dias);
        return new Prazo(inicio, inicio.AddDays(dias));
    }
}
