using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

/// <summary>
/// Janela de vigencia de uma atribuicao/concessao (ferias, mandato, designacao temporaria):
/// intervalo <c>[Inicio, Fim]</c> com fim opcional (aberto = sem termino). Objeto de Valor
/// imutavel comparado por igualdade estrutural (MODELO §2.2, invariante I8).
/// </summary>
public sealed class Vigencia : ValueObject
{
    private Vigencia(DateTimeOffset inicio, DateTimeOffset? fim)
    {
        Inicio = inicio;
        Fim = fim;
    }

    /// <summary>Instante de inicio da vigencia.</summary>
    public DateTimeOffset Inicio { get; }

    /// <summary>Instante de termino (inclusive); <c>null</c> = vigencia aberta/permanente.</summary>
    public DateTimeOffset? Fim { get; }

    /// <summary>Cria uma vigencia. O fim, se informado, nao pode ser anterior ao inicio.</summary>
    /// <param name="inicio">Inicio da vigencia.</param>
    /// <param name="fim">Fim opcional.</param>
    /// <returns>Nova <see cref="Vigencia"/>.</returns>
    /// <exception cref="ArgumentException">Se <paramref name="fim"/> for anterior a <paramref name="inicio"/>.</exception>
    public static Vigencia Criar(DateTimeOffset inicio, DateTimeOffset? fim = null)
    {
        if (fim is not null && fim < inicio)
        {
            throw new ArgumentException("O fim da vigencia nao pode ser anterior ao inicio.", nameof(fim));
        }

        return new Vigencia(inicio, fim);
    }

    /// <summary>Cria uma vigencia aberta (sem termino) a partir de agora.</summary>
    /// <param name="agora">Instante de inicio.</param>
    /// <returns>Nova <see cref="Vigencia"/> sem fim.</returns>
    public static Vigencia Aberta(DateTimeOffset agora) => new(agora, fim: null);

    /// <summary>Indica se a vigencia esta vigente no instante informado.</summary>
    /// <param name="instante">Momento de referencia.</param>
    /// <returns><c>true</c> se <c>Inicio &lt;= instante</c> e (sem fim ou <c>instante &lt;= Fim</c>).</returns>
    public bool VigenteEm(DateTimeOffset instante)
        => instante >= Inicio && (Fim is null || instante <= Fim);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return Fim;
    }
}
