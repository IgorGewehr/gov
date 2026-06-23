namespace Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

/// <summary>
/// Politica de (sub)delegacao do tenant (AA-5/D3): limita a PROFUNDIDADE da cadeia de subdelegacao
/// ao conceder papeis/escopo, barrando cadeias infinitas em que um delegado cria outro delegado
/// indefinidamente. O limite e PARAMETRIZAVEL por tenant (CLAUDE.md §7: regras de autorizacao nunca
/// hardcoded); um teto conservador e aplicado por padrao. Objeto de Valor imutavel, sem I/O.
/// </summary>
/// <remarks>
/// Profundidade 0 = atribuicao DIRETA (admin do tenant, raiz da cadeia). Cada subdelegacao herda a
/// profundidade do poder do concedente + 1. <see cref="ProfundidadeMaxima"/> e a maior profundidade
/// ADMISSIVEL: uma concessao cuja profundidade resultante a ultrapasse e NEGADA.
/// </remarks>
public sealed record PoliticaDelegacao
{
    /// <summary>
    /// Teto padrao da profundidade de subdelegacao quando o tenant nao define um valor proprio.
    /// Conservador: admin direto (0) pode delegar (1) e o delegado pode subdelegar uma vez (2),
    /// mas nao alem — contendo a propagacao lateral (amplificador de AA-1/AA-2 apontado no RED-TEAM).
    /// </summary>
    public const int ProfundidadeMaximaPadrao = 2;

    private PoliticaDelegacao(int profundidadeMaxima) => ProfundidadeMaxima = profundidadeMaxima;

    /// <summary>Maior profundidade de subdelegacao admissivel (0 = so atribuicoes diretas).</summary>
    public int ProfundidadeMaxima { get; }

    /// <summary>Politica padrao (<see cref="ProfundidadeMaximaPadrao"/>).</summary>
    public static PoliticaDelegacao Padrao { get; } = new(ProfundidadeMaximaPadrao);

    /// <summary>Cria a politica com o teto de profundidade do tenant.</summary>
    /// <param name="profundidadeMaxima">Maior profundidade admissivel (>= 0).</param>
    /// <returns>Politica de delegacao.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o teto for negativo.</exception>
    public static PoliticaDelegacao Com(int profundidadeMaxima)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(profundidadeMaxima);
        return new PoliticaDelegacao(profundidadeMaxima);
    }

    /// <summary>Indica se uma concessao com a profundidade resultante informada e permitida pelo teto.</summary>
    /// <param name="profundidadeResultante">Profundidade que a nova atribuicao teria.</param>
    /// <returns><c>true</c> se cabe no teto; <c>false</c> se o excede.</returns>
    public bool Permite(int profundidadeResultante) => profundidadeResultante <= ProfundidadeMaxima;
}
