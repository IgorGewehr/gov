using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

/// <summary>
/// Faixa populacional do art. 29-A da CF/88 (EC 25/2000 e EC 58/2009): associa um intervalo de habitantes
/// a um percentual-limite da receita sobre o qual incide o teto de despesa total do Poder Legislativo
/// municipal. VO imutavel comparado por valor.
/// <para>
/// O intervalo e representado pelo seu <b>limite superior INCLUSIVE</b> de habitantes
/// (<see cref="PopulacaoMaximaInclusive"/>); a ultima faixa (sem teto superior) usa
/// <see cref="int.MaxValue"/>. Os percentuais NAO sao hardcoded no dominio — vem da configuracao por
/// tenant (norma-fonte) e sao apenas validados aqui (0 &lt; p &lt;= 1).
/// </para>
/// <para>
/// REFERENCIA (defaults tipicos pos-EC 58/2009, parametrizaveis): ate 100.000 hab = 7%; 100.001 a
/// 300.000 = 6%; 300.001 a 500.000 = 5%; 500.001 a 3.000.000 = 4,5%; 3.000.001 a 8.000.000 = 4%; acima
/// de 8.000.000 = 3,5%. A BORDA de 100.000 e explicita (inclusive na primeira faixa).
/// </para>
/// </summary>
public sealed class FaixaPopulacional : ValueObject
{
    private FaixaPopulacional(int populacaoMaximaInclusive, decimal percentual)
    {
        PopulacaoMaximaInclusive = populacaoMaximaInclusive;
        Percentual = percentual;
    }

    /// <summary>Limite superior INCLUSIVE de habitantes da faixa (<see cref="int.MaxValue"/> = sem teto).</summary>
    public int PopulacaoMaximaInclusive { get; }

    /// <summary>Percentual-limite da receita aplicavel a faixa (fracao: 0 &lt; p &lt;= 1; ex.: 0,07 = 7%).</summary>
    public decimal Percentual { get; }

    /// <summary>
    /// Cria uma faixa validada.
    /// </summary>
    /// <param name="populacaoMaximaInclusive">Limite superior inclusive de habitantes (positivo).</param>
    /// <param name="percentual">Percentual da receita (fracao em (0, 1]).</param>
    /// <returns>Faixa populacional validada.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a populacao nao for positiva ou o percentual sair de (0, 1].</exception>
    public static FaixaPopulacional De(int populacaoMaximaInclusive, decimal percentual)
    {
        if (populacaoMaximaInclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(populacaoMaximaInclusive), "Limite de habitantes da faixa deve ser positivo.");
        }

        if (percentual is <= 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(percentual), "Percentual da faixa deve estar em (0, 1].");
        }

        return new FaixaPopulacional(populacaoMaximaInclusive, percentual);
    }

    /// <summary>Indica se a populacao informada se enquadra nesta faixa (&lt;= limite superior inclusive).</summary>
    /// <param name="populacao">Populacao do municipio (positiva).</param>
    /// <returns><c>true</c> se a populacao for menor ou igual ao limite superior da faixa.</returns>
    public bool Comporta(int populacao) => populacao <= PopulacaoMaximaInclusive;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PopulacaoMaximaInclusive;
        yield return Percentual;
    }
}
