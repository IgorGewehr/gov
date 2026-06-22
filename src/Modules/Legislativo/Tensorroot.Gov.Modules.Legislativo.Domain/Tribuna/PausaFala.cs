using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;

/// <summary>
/// Intervalo de pausa (inicio/fim) de uma fala na tribuna — objeto de valor. Descontado do tempo
/// utilizado pelo orador (timestamps do servidor, T-5/T-8).
/// </summary>
public sealed class PausaFala : ValueObject
{
    private PausaFala(DateTimeOffset inicio, DateTimeOffset fim)
    {
        Inicio = inicio;
        Fim = fim;
    }

    /// <summary>Momento de inicio da pausa.</summary>
    public DateTimeOffset Inicio { get; }

    /// <summary>Momento de fim da pausa.</summary>
    public DateTimeOffset Fim { get; }

    /// <summary>Duracao da pausa.</summary>
    public TimeSpan Duracao => Fim - Inicio;

    /// <summary>Cria um intervalo de pausa valido (fim nao anterior ao inicio).</summary>
    /// <param name="inicio">Inicio da pausa.</param>
    /// <param name="fim">Fim da pausa.</param>
    /// <returns>Nova <see cref="PausaFala"/>.</returns>
    /// <exception cref="ArgumentException">Se o fim for anterior ao inicio.</exception>
    public static PausaFala De(DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (fim < inicio)
        {
            throw new ArgumentException("Fim da pausa nao pode ser anterior ao inicio.", nameof(fim));
        }

        return new PausaFala(inicio, fim);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return Fim;
    }
}
