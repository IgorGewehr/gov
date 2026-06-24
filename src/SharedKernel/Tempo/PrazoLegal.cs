using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.SharedKernel.Tempo;

/// <summary>Unidade de contagem de um prazo legal.</summary>
public enum UnidadePrazo
{
    /// <summary>Dias uteis (pula fins de semana e feriados do tenant).</summary>
    DiasUteis = 1,

    /// <summary>Dias corridos (calendario direto; vencimento em dia nao util rola para o proximo util).</summary>
    DiasCorridos = 2,
}

/// <summary>
/// Prazo legal: "X dias (uteis|corridos) a partir de Y", com norma-fonte citavel (para auditoria/TCE).
/// Imutavel, reproduzivel (recebe o calendario + as datas; NUNCA le relogio). Resolve o
/// <see cref="Vencimento"/> UMA vez e responde "vencido?/a vencer?" sem reler o calendario. Reusado por
/// W9.1 (PNCP), W9.3 (Obras), W9.6 (Convenios/MROSC) e e-SIC, com a quantidade/unidade/norma vindas
/// dos parametros do tenant (sem numero magico — CLAUDE.md S7/S16).
/// </summary>
public sealed class PrazoLegal : ValueObject
{
    private PrazoLegal(DateOnly inicio, int quantidade, UnidadePrazo unidade, DateOnly vencimento, string normaFonte)
    {
        Inicio = inicio;
        Quantidade = quantidade;
        Unidade = unidade;
        Vencimento = vencimento;
        NormaFonte = normaFonte;
    }

    /// <summary>Data base a partir da qual o prazo e contado.</summary>
    public DateOnly Inicio { get; }

    /// <summary>Quantidade de dias (uteis ou corridos, conforme <see cref="Unidade"/>).</summary>
    public int Quantidade { get; }

    /// <summary>Unidade de contagem (uteis|corridos).</summary>
    public UnidadePrazo Unidade { get; }

    /// <summary>
    /// Data-limite ja resolvida (em dias uteis: via calendario; em corridos: <c>AddDays</c> + rolagem para
    /// o proximo dia util quando cai em dia nao util).
    /// </summary>
    public DateOnly Vencimento { get; }

    /// <summary>
    /// Citacao legal (ex.: "Lei 14.133 art. 94"). Sem numero magico solto — a fonte viaja com o prazo
    /// (auditavel pelo Tribunal de Contas).
    /// </summary>
    public string NormaFonte { get; }

    /// <summary>
    /// Reidrata um prazo a partir de valores JA RESOLVIDOS (persistencia). NAO recalcula o vencimento —
    /// usado por mapeamentos EF para reconstruir o VO sem reler o calendario (o vencimento persistido e a
    /// fonte de verdade do momento da celebracao; feriados retroativos nao reescrevem o ato ja praticado).
    /// </summary>
    /// <param name="inicio">Data base original.</param>
    /// <param name="quantidade">Quantidade de dias original.</param>
    /// <param name="unidade">Unidade de contagem original.</param>
    /// <param name="vencimento">Vencimento JA resolvido (persistido).</param>
    /// <param name="normaFonte">Norma-fonte original.</param>
    /// <returns>Prazo reconstruido com o vencimento persistido.</returns>
    public static PrazoLegal Reidratar(
        DateOnly inicio,
        int quantidade,
        UnidadePrazo unidade,
        DateOnly vencimento,
        string normaFonte)
        => new(inicio, quantidade, unidade, vencimento, normaFonte);

    /// <summary>
    /// Constroi o prazo resolvendo o vencimento a partir de <paramref name="inicio"/>. Dias uteis ⇒
    /// <c>calendario.AdicionarDiasUteis</c>; corridos ⇒ <c>AddDays(n)</c> e, se cair em dia nao util,
    /// prorroga para o proximo dia util (Lei 14.133 art. 110 / art. 224 CPC).
    /// </summary>
    /// <param name="inicio">Data base do prazo.</param>
    /// <param name="quantidade">Quantidade de dias (&gt;= 0) — vem dos parametros do tenant.</param>
    /// <param name="unidade">Unidade de contagem (uteis|corridos) — vem dos parametros do tenant.</param>
    /// <param name="normaFonte">Citacao legal nao vazia (norma-fonte do prazo).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <returns>Novo <see cref="PrazoLegal"/> com o vencimento resolvido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="quantidade"/> for negativa.</exception>
    /// <exception cref="ArgumentException">Se <paramref name="normaFonte"/> for vazia.</exception>
    /// <exception cref="ArgumentNullException">Se <paramref name="calendario"/> for nulo.</exception>
    public static PrazoLegal Criar(
        DateOnly inicio,
        int quantidade,
        UnidadePrazo unidade,
        string normaFonte,
        ICalendarioDiasUteis calendario)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidade);
        ArgumentException.ThrowIfNullOrWhiteSpace(normaFonte);
        ArgumentNullException.ThrowIfNull(calendario);

        var vencimento = unidade == UnidadePrazo.DiasUteis
            ? calendario.AdicionarDiasUteis(inicio, quantidade)
            : calendario.ProximoDiaUtil(inicio.AddDays(quantidade));

        return new PrazoLegal(inicio, quantidade, unidade, vencimento, normaFonte.Trim());
    }

    /// <summary>
    /// Vencido em <paramref name="hoje"/> (<paramref name="hoje"/> &gt; <see cref="Vencimento"/>).
    /// O <paramref name="hoje"/> vem do <c>TimeProvider</c> no Application — nunca de dentro do prazo.
    /// </summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se ja vencido.</returns>
    public bool Vencido(DateOnly hoje) => hoje > Vencimento;

    /// <summary>
    /// A vencer em <paramref name="hoje"/> (ainda dentro do prazo, vencimento incluido —
    /// <paramref name="hoje"/> &lt;= <see cref="Vencimento"/>).
    /// </summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se ainda dentro do prazo.</returns>
    public bool AVencer(DateOnly hoje) => hoje <= Vencimento;

    /// <summary>Dias uteis restantes ate o vencimento (negativo se ja vencido).</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <returns>Quantidade de dias uteis ate o vencimento.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="calendario"/> for nulo.</exception>
    public int DiasUteisRestantes(DateOnly hoje, ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        return calendario.DiasUteisEntre(hoje, Vencimento);
    }

    /// <summary>
    /// Prorroga UMA aplicacao adicional a partir do vencimento atual (ex.: e-SIC +10 d.u.; PC OSC +30 d),
    /// preservando a unidade. Devolve um NOVO value object (imutabilidade).
    /// </summary>
    /// <param name="quantidadeAdicional">Quantidade adicional (&gt;= 0).</param>
    /// <param name="normaFonte">Norma-fonte da prorrogacao.</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <returns>Novo <see cref="PrazoLegal"/> com o vencimento prorrogado.</returns>
    public PrazoLegal Prorrogar(int quantidadeAdicional, string normaFonte, ICalendarioDiasUteis calendario)
        => Criar(Vencimento, quantidadeAdicional, Unidade, normaFonte, calendario);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return Quantidade;
        yield return (int)Unidade;
        yield return Vencimento;
        yield return NormaFonte;
    }
}
