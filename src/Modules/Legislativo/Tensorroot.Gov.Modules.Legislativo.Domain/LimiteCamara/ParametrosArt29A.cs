using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

/// <summary>
/// Conjunto PARAMETRIZAVEL (por tenant, com norma-fonte) das regras numericas do art. 29-A da CF/88,
/// reunido num VO para que a apuracao nunca dependa de numero magico:
/// <list type="bullet">
///   <item>a tabela de faixas populacionais x percentual (caput, incisos I a VI — EC 25/2000, EC 58/2009);</item>
///   <item>o subteto da FOLHA da Camara como fracao do repasse/duodecimo (§1: max. 70%);</item>
///   <item>o limiar de ATENCAO do semaforo (fracao de utilizacao a partir da qual sinaliza amarelo);</item>
///   <item>o exercicio-corte da EC 109/2021 (art. 7º): inativos/pensionistas so entram no teto a partir
///         da legislatura iniciada em 2025.</item>
/// </list>
/// </summary>
public sealed class ParametrosArt29A : ValueObject
{
    private readonly IReadOnlyList<FaixaPopulacional> _faixasOrdenadas;

    private ParametrosArt29A(
        IReadOnlyList<FaixaPopulacional> faixasOrdenadas,
        decimal subtetoFolhaSobreRepasse,
        decimal limiarAtencao,
        int exercicioCorteInativos)
    {
        _faixasOrdenadas = faixasOrdenadas;
        SubtetoFolhaSobreRepasse = subtetoFolhaSobreRepasse;
        LimiarAtencao = limiarAtencao;
        ExercicioCorteInativos = exercicioCorteInativos;
    }

    /// <summary>Subteto da folha da Camara como fracao do repasse (§1: tipicamente 0,70 = 70%).</summary>
    public decimal SubtetoFolhaSobreRepasse { get; }

    /// <summary>Fracao de utilizacao do teto a partir da qual o semaforo passa a ATENCAO (ex.: 0,95).</summary>
    public decimal LimiarAtencao { get; }

    /// <summary>Exercicio a partir do qual inativos/pensionistas integram o teto (EC 109/2021 art. 7º: 2025).</summary>
    public int ExercicioCorteInativos { get; }

    /// <summary>Faixas populacionais ordenadas por limite superior crescente.</summary>
    public IReadOnlyList<FaixaPopulacional> Faixas => _faixasOrdenadas;

    /// <summary>
    /// Monta os parametros validados: ordena as faixas, garante que cobrem qualquer populacao (a ultima
    /// faixa deve ser aberta superiormente) e valida o subteto/limiar.
    /// </summary>
    /// <param name="faixas">Faixas populacionais x percentual (nao vazio).</param>
    /// <param name="subtetoFolhaSobreRepasse">Subteto da folha sobre o repasse (fracao em (0, 1]).</param>
    /// <param name="limiarAtencao">Limiar de atencao do semaforo (fracao em (0, 1]).</param>
    /// <param name="exercicioCorteInativos">Exercicio-corte da EC 109/2021 (positivo).</param>
    /// <returns>Parametros validados.</returns>
    /// <exception cref="ArgumentException">Se as faixas forem vazias ou nao cobrirem toda populacao.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se subteto/limiar/exercicio sairem dos limites validos.</exception>
    public static ParametrosArt29A De(
        IEnumerable<FaixaPopulacional> faixas,
        decimal subtetoFolhaSobreRepasse,
        decimal limiarAtencao,
        int exercicioCorteInativos)
    {
        ArgumentNullException.ThrowIfNull(faixas);

        var ordenadas = faixas.OrderBy(faixa => faixa.PopulacaoMaximaInclusive).ToList();
        if (ordenadas.Count == 0)
        {
            throw new ArgumentException("E necessaria ao menos uma faixa populacional.", nameof(faixas));
        }

        // A ultima faixa tem de ser aberta superiormente (int.MaxValue) para cobrir qualquer municipio.
        if (ordenadas[^1].PopulacaoMaximaInclusive != int.MaxValue)
        {
            throw new ArgumentException("A ultima faixa populacional deve ser aberta superiormente (sem teto de habitantes).", nameof(faixas));
        }

        if (subtetoFolhaSobreRepasse is <= 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(subtetoFolhaSobreRepasse), "Subteto da folha deve estar em (0, 1].");
        }

        if (limiarAtencao is <= 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(limiarAtencao), "Limiar de atencao deve estar em (0, 1].");
        }

        if (exercicioCorteInativos <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exercicioCorteInativos), "Exercicio-corte deve ser positivo.");
        }

        return new ParametrosArt29A(ordenadas, subtetoFolhaSobreRepasse, limiarAtencao, exercicioCorteInativos);
    }

    /// <summary>
    /// Resolve o percentual-limite aplicavel a uma populacao (primeira faixa que a comporta).
    /// </summary>
    /// <param name="populacao">Populacao do municipio (positiva).</param>
    /// <returns>A faixa aplicavel.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a populacao nao for positiva.</exception>
    public FaixaPopulacional ResolverFaixa(int populacao)
    {
        if (populacao <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(populacao), "Populacao deve ser positiva.");
        }

        // Faixas ordenadas crescentes: a primeira que comporta a populacao e a aplicavel.
        foreach (var faixa in _faixasOrdenadas)
        {
            if (faixa.Comporta(populacao))
            {
                return faixa;
            }
        }

        // Inalcancavel: a ultima faixa e aberta (int.MaxValue) por invariante de construcao.
        return _faixasOrdenadas[^1];
    }

    /// <summary>
    /// Indica se inativos/pensionistas integram o teto no exercicio informado (EC 109/2021 art. 7º:
    /// somente a partir do <see cref="ExercicioCorteInativos"/>).
    /// </summary>
    /// <param name="exercicio">Exercicio de apuracao.</param>
    /// <returns><c>true</c> se inativos/pensionistas contam para o teto neste exercicio.</returns>
    public bool InativosContamNoTeto(int exercicio) => exercicio >= ExercicioCorteInativos;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SubtetoFolhaSobreRepasse;
        yield return LimiarAtencao;
        yield return ExercicioCorteInativos;
        foreach (var faixa in _faixasOrdenadas)
        {
            yield return faixa;
        }
    }
}
