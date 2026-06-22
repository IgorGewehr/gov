namespace Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

/// <summary>
/// Cota/limite de vagas aplicavel a um procedimento na competencia/unidade. Value Object imutavel.
/// Snapshot do consumo no momento da regulacao (a fonte de verdade pode ser tabela propria de cota).
/// </summary>
/// <param name="Disponivel">Vagas ainda disponiveis (>= 0).</param>
/// <param name="Total">Total de vagas da cota (>= disponivel).</param>
public readonly record struct Cota(int Disponivel, int Total)
{
    /// <summary>Cria uma cota validada (disponivel e total nao negativos, disponivel &lt;= total).</summary>
    /// <param name="disponivel">Vagas disponiveis.</param>
    /// <param name="total">Total de vagas.</param>
    /// <returns>Instancia de <see cref="Cota"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se os limites forem invalidos.</exception>
    public static Cota Criar(int disponivel, int total)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(disponivel);
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(disponivel, total);
        return new Cota(disponivel, total);
    }

    /// <summary>Indica se ha ao menos uma vaga disponivel.</summary>
    /// <returns><c>true</c> se <see cref="Disponivel"/> &gt; 0.</returns>
    public bool TemDisponibilidade() => Disponivel > 0;

    /// <summary>Consome uma vaga (decrementa a disponibilidade).</summary>
    /// <returns>Nova cota com uma vaga a menos.</returns>
    /// <exception cref="InvalidOperationException">Se nao houver disponibilidade.</exception>
    public Cota ConsumirVaga()
    {
        if (!TemDisponibilidade())
        {
            throw new InvalidOperationException("Cota esgotada: nao ha vaga disponivel para consumir.");
        }

        return new Cota(Disponivel - 1, Total);
    }

    /// <summary>Devolve uma vaga (incrementa a disponibilidade, limitado ao total).</summary>
    /// <returns>Nova cota com uma vaga a mais.</returns>
    public Cota DevolverVaga() => new(Math.Min(Disponivel + 1, Total), Total);
}
