using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

/// <summary>
/// SNAPSHOT dos parametros do art. 29-A ja RESOLVIDOS para uma apuracao concreta: o percentual da faixa
/// (resolvido pela populacao no momento da abertura), o subteto da folha (§1), o limiar de atencao do
/// semaforo e o exercicio-corte da EC 109/2021. Congela a regra numerica vigente quando a apuracao foi
/// aberta — auditavel e estavel mesmo que a parametrizacao do tenant mude depois.
/// <para>
/// VO de escalares (sem colecoes) — persistivel como colunas owned simples, sem fragilidade de
/// materializacao. A tabela de faixas vive na configuracao (<c>ParametrosArt29A</c>); aqui guarda-se so o
/// resultado aplicavel a esta apuracao.
/// </para>
/// </summary>
public sealed class ParametrosResolvidosArt29A : ValueObject
{
    private ParametrosResolvidosArt29A(
        decimal percentualFaixa,
        decimal subtetoFolhaSobreRepasse,
        decimal limiarAtencao,
        int exercicioCorteInativos)
    {
        PercentualFaixa = percentualFaixa;
        SubtetoFolhaSobreRepasse = subtetoFolhaSobreRepasse;
        LimiarAtencao = limiarAtencao;
        ExercicioCorteInativos = exercicioCorteInativos;
    }

    /// <summary>Percentual-limite da faixa populacional aplicada (fracao; ex.: 0,07).</summary>
    public decimal PercentualFaixa { get; }

    /// <summary>Subteto da folha como fracao do repasse (§1; ex.: 0,70).</summary>
    public decimal SubtetoFolhaSobreRepasse { get; }

    /// <summary>Fracao de utilizacao do teto a partir da qual o semaforo passa a ATENCAO.</summary>
    public decimal LimiarAtencao { get; }

    /// <summary>Exercicio-corte da EC 109/2021 (inativos/pensionistas no teto a partir dele).</summary>
    public int ExercicioCorteInativos { get; }

    /// <summary>
    /// Resolve o snapshot a partir dos parametros configurados do tenant e da populacao do municipio.
    /// </summary>
    /// <param name="parametros">Parametros configurados (faixas/subteto/limiar/corte).</param>
    /// <param name="populacao">Populacao do municipio (resolve a faixa).</param>
    /// <returns>Snapshot resolvido para a apuracao.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="parametros"/> for nulo.</exception>
    public static ParametrosResolvidosArt29A Resolver(ParametrosArt29A parametros, int populacao)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        var faixa = parametros.ResolverFaixa(populacao);
        return new ParametrosResolvidosArt29A(
            faixa.Percentual,
            parametros.SubtetoFolhaSobreRepasse,
            parametros.LimiarAtencao,
            parametros.ExercicioCorteInativos);
    }

    /// <summary>Indica se inativos/pensionistas integram o teto no exercicio informado (EC 109/2021).</summary>
    /// <param name="exercicio">Exercicio de apuracao.</param>
    /// <returns><c>true</c> se inativos/pensionistas contam para o teto neste exercicio.</returns>
    public bool InativosContamNoTeto(int exercicio) => exercicio >= ExercicioCorteInativos;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PercentualFaixa;
        yield return SubtetoFolhaSobreRepasse;
        yield return LimiarAtencao;
        yield return ExercicioCorteInativos;
    }
}
