using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

/// <summary>Identificador forte do agregado <see cref="ApuracaoArt29A"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ApuracaoArt29AId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ApuracaoArt29AId"/>.</returns>
    public static ApuracaoArt29AId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situacao do demonstrativo de apuracao do art. 29-A.</summary>
public enum SituacaoApuracaoArt29A
{
    /// <summary>Em elaboracao (despesas sendo lancadas; pode ser editada).</summary>
    Rascunho = 1,

    /// <summary>Consolidada (demonstrativo fechado para o exercicio; pronto para prestacao ao TCE — M10).</summary>
    Consolidada = 2,
}

/// <summary>
/// Apuracao do limite de despesa TOTAL do Poder Legislativo municipal (CF/88 art. 29-A), por exercicio e
/// por tenant (Camara). Raiz de agregado que reune as ENTRADAS (populacao, base de receita do exercicio
/// anterior, repasse/duodecimo recebido, parametros de faixas/subteto) e a despesa REALIZADA discriminada
/// por natureza, e deriva o veredito: teto da despesa total (caput) + subteto da folha sobre o repasse
/// (§1, max. 70%), com semaforo e trilha imutavel de auditoria.
/// <para>
/// Regra temporal EC 109/2021 (art. 7º): inativos/pensionistas so integram o teto a partir do exercicio
/// de corte parametrizado (legislatura de 2025). Tudo parametrizavel — nenhum percentual e hardcoded
/// (CLAUDE.md §7).
/// </para>
/// </summary>
public sealed class ApuracaoArt29A : AggregateRoot<ApuracaoArt29AId>, IMustHaveTenant
{
    private readonly List<ItemDespesaCamara> _despesas = [];

    private ApuracaoArt29A()
    {
    }

    private ApuracaoArt29A(
        ApuracaoArt29AId id,
        Guid tenantId,
        int exercicio,
        int populacao,
        ReceitaBaseArt29A baseReceita,
        decimal repasseRecebido,
        ParametrosResolvidosArt29A parametros)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        Populacao = populacao;
        BaseReceita = baseReceita;
        RepasseRecebido = repasseRecebido;
        Parametros = parametros;
        Situacao = SituacaoApuracaoArt29A.Rascunho;
        RaiseDomainEvent(new ApuracaoArt29AAberta(Id, exercicio));
    }

    /// <summary>Tenant (Camara Municipal) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercicio orcamentario da Camara sob teto.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Populacao do municipio (define a faixa/percentual do caput).</summary>
    public int Populacao { get; private set; }

    /// <summary>Base de receita do exercicio anterior (tributaria + transferencias).</summary>
    public ReceitaBaseArt29A BaseReceita { get; private set; } = default!;

    /// <summary>Repasse/duodecimo efetivamente recebido pela Camara no exercicio (base do subteto §1).</summary>
    public decimal RepasseRecebido { get; private set; }

    /// <summary>Parametros RESOLVIDOS (percentual da faixa/subteto/limiar/corte EC109) — snapshot na abertura.</summary>
    public ParametrosResolvidosArt29A Parametros { get; private set; } = default!;

    /// <summary>Situacao do demonstrativo.</summary>
    public SituacaoApuracaoArt29A Situacao { get; private set; }

    /// <summary>Despesas realizadas discriminadas por natureza.</summary>
    public IReadOnlyList<ItemDespesaCamara> Despesas => _despesas;

    /// <summary>
    /// Abre uma nova apuracao do art. 29-A (situacao Rascunho), validando as entradas e capturando os
    /// parametros vigentes do tenant.
    /// </summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="exercicio">Exercicio orcamentario sob teto.</param>
    /// <param name="populacao">Populacao do municipio (positiva).</param>
    /// <param name="baseReceita">Base de receita do exercicio anterior.</param>
    /// <param name="repasseRecebido">Repasse/duodecimo recebido (&gt;= 0).</param>
    /// <param name="parametros">Parametros de faixas/subteto vigentes.</param>
    /// <returns>Nova apuracao em Rascunho.</returns>
    /// <exception cref="ArgumentNullException">Se base/parametros forem nulos.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se exercicio/populacao/repasse forem invalidos.</exception>
    public static ApuracaoArt29A Abrir(
        Guid tenantId,
        int exercicio,
        int populacao,
        ReceitaBaseArt29A baseReceita,
        decimal repasseRecebido,
        ParametrosArt29A parametros)
    {
        ArgumentNullException.ThrowIfNull(baseReceita);
        ArgumentNullException.ThrowIfNull(parametros);

        if (exercicio <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exercicio), "Exercicio deve ser positivo.");
        }

        if (populacao <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(populacao), "Populacao deve ser positiva.");
        }

        if (repasseRecebido < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(repasseRecebido), "Repasse recebido nao pode ser negativo.");
        }

        // O caput toma a receita do exercicio ANTERIOR: a base de referencia deve ser exercicio - 1.
        if (baseReceita.ExercicioReferencia != exercicio - 1)
        {
            throw new ArgumentException(
                $"A base de receita deve ser do exercicio anterior ({exercicio - 1}); recebida {baseReceita.ExercicioReferencia}.",
                nameof(baseReceita));
        }

        // Congela (snapshot) os parametros aplicaveis a esta apuracao: percentual ja resolvido pela populacao.
        var resolvidos = ParametrosResolvidosArt29A.Resolver(parametros, populacao);
        return new ApuracaoArt29A(ApuracaoArt29AId.New(), tenantId, exercicio, populacao, baseReceita, repasseRecebido, resolvidos);
    }

    /// <summary>
    /// Lanca uma parcela de despesa realizada (so em Rascunho). Append-only enquanto editavel.
    /// </summary>
    /// <param name="natureza">Natureza da despesa.</param>
    /// <param name="valor">Valor realizado (&gt;= 0).</param>
    /// <param name="descricao">Descricao (opcional).</param>
    /// <exception cref="InvalidOperationException">Se a apuracao ja estiver consolidada.</exception>
    public void LancarDespesa(NaturezaDespesaCamara natureza, decimal valor, string? descricao = null)
    {
        if (Situacao == SituacaoApuracaoArt29A.Consolidada)
        {
            throw new InvalidOperationException("Apuracao consolidada nao admite lancamento de despesa.");
        }

        _despesas.Add(ItemDespesaCamara.De(natureza, valor, descricao));
    }

    /// <summary>
    /// Apura o veredito do art. 29-A (read-only/derivado): resolve a faixa pela populacao, calcula o teto
    /// (base x percentual) e o subteto da folha (repasse x §1), soma a despesa total sujeita ao teto
    /// (aplicando a regra temporal EC 109 aos inativos/pensionistas) e a folha, e classifica os semaforos.
    /// </summary>
    /// <returns>Veredito calculado da apuracao.</returns>
    public ResultadoApuracaoArt29A Apurar()
    {
        var percentualFaixa = Parametros.PercentualFaixa;
        var teto = BaseReceita.Total * percentualFaixa;

        var inativosNoTeto = Parametros.InativosContamNoTeto(Exercicio);

        // Despesa total sujeita ao teto: tudo, EXCETO inativos/pensionistas antes do corte da EC 109/2021.
        var despesaTotal = _despesas
            .Where(item => inativosNoTeto || item.Natureza != NaturezaDespesaCamara.InativosPensionistas)
            .Sum(item => item.Valor);

        // Folha sujeita ao subteto §1: pessoal ativo + inativos/pensionistas elegiveis (mesma regra temporal).
        var folha = _despesas
            .Where(item => item.EhFolha)
            .Where(item => inativosNoTeto || item.Natureza != NaturezaDespesaCamara.InativosPensionistas)
            .Sum(item => item.Valor);

        var subtetoFolha = RepasseRecebido * Parametros.SubtetoFolhaSobreRepasse;

        var semaforoTeto = Classificar(despesaTotal, teto);
        var semaforoFolha = Classificar(folha, subtetoFolha);

        return new ResultadoApuracaoArt29A(
            percentualFaixa,
            BaseReceita.Total,
            teto,
            despesaTotal,
            teto - despesaTotal,
            UtilizacaoFracao(despesaTotal, teto),
            semaforoTeto,
            RepasseRecebido,
            subtetoFolha,
            folha,
            subtetoFolha - folha,
            UtilizacaoFracao(folha, subtetoFolha),
            semaforoFolha,
            inativosNoTeto);
    }

    /// <summary>
    /// Consolida o demonstrativo (Rascunho -&gt; Consolidada, terminal): congela a apuracao para a prestacao
    /// ao TCE-RS (transmissao real = M10) e emite o evento com os semaforos.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se ja estiver consolidada.</exception>
    public ResultadoApuracaoArt29A Consolidar()
    {
        if (Situacao == SituacaoApuracaoArt29A.Consolidada)
        {
            throw new InvalidOperationException("Apuracao ja consolidada.");
        }

        var resultado = Apurar();
        Situacao = SituacaoApuracaoArt29A.Consolidada;
        RaiseDomainEvent(new ApuracaoArt29AConsolidada(Id, Exercicio, resultado.SemaforoTeto, resultado.SemaforoFolha));
        return resultado;
    }

    // Semaforo: Excedido quando realizado > limite; Atencao a partir do limiar parametrizado; senao Adequado.
    private SemaforoLimite Classificar(decimal realizado, decimal limite)
    {
        if (limite <= 0m)
        {
            // Sem limite positivo (ex.: repasse zero) qualquer despesa positiva ja excede; zero e adequado.
            return realizado > 0m ? SemaforoLimite.Excedido : SemaforoLimite.Adequado;
        }

        if (realizado > limite)
        {
            return SemaforoLimite.Excedido;
        }

        return realizado / limite >= Parametros.LimiarAtencao
            ? SemaforoLimite.Atencao
            : SemaforoLimite.Adequado;
    }

    private static decimal UtilizacaoFracao(decimal realizado, decimal limite)
        => limite <= 0m ? 0m : realizado / limite;
}
