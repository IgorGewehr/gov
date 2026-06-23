using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

/// <summary>Identificador forte da entidade-filha <see cref="AtribuicaoDePapel"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AtribuicaoDePapelId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AtribuicaoDePapelId"/>.</returns>
    public static AtribuicaoDePapelId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Atribuicao de um <see cref="Papel"/> a um <see cref="Usuario"/> COM ESCOPO organizacional
/// (ABAC do sujeito — MODELO §2.2). Substitui o antigo <c>HashSet&lt;PapelId&gt;</c> plano: as
/// permissoes efetivas deixam de ser globais e passam a ser <c>(permissao, conjunto de UOs)</c>.
/// E entidade-filha do agregado <see cref="Usuario"/> (criada/revogada SOMENTE pela raiz).
/// </summary>
/// <remarks>
/// O escopo e a subarvore enraizada em <see cref="UnidadeId"/>: se <see cref="IncluiSubunidades"/>
/// for <c>true</c>, o papel vale na UO e em todos os descendentes (invariante I5; a expansao
/// concreta da subarvore e responsabilidade do enforcement/aplicacao, nao do agregado).
/// </remarks>
public sealed class AtribuicaoDePapel : Entity<AtribuicaoDePapelId>
{
    private AtribuicaoDePapel()
    {
    }

    private AtribuicaoDePapel(
        AtribuicaoDePapelId id,
        PapelId papelId,
        UnidadeOrganizacionalId unidadeId,
        bool incluiSubunidades,
        Vigencia vigencia,
        OrigemAtribuicao origem,
        int profundidadeDelegacao)
        : base(id)
    {
        PapelId = papelId;
        UnidadeId = unidadeId;
        IncluiSubunidades = incluiSubunidades;
        Vigencia = vigencia;
        Origem = origem;
        ProfundidadeDelegacao = profundidadeDelegacao;
    }

    /// <summary>Papel (perfil RBAC) atribuido.</summary>
    public PapelId PapelId { get; private set; }

    /// <summary>UO raiz da subarvore em que o papel vale.</summary>
    public UnidadeOrganizacionalId UnidadeId { get; private set; }

    /// <summary><c>true</c> → vale na UO e em todos os descendentes; <c>false</c> → so na UO exata.</summary>
    public bool IncluiSubunidades { get; private set; }

    /// <summary>Janela temporal da atribuicao.</summary>
    public Vigencia Vigencia { get; private set; } = default!;

    /// <summary>Procedencia (direta/delegada + concedente).</summary>
    public OrigemAtribuicao Origem { get; private set; } = default!;

    /// <summary>
    /// Profundidade da cadeia de (sub)delegacao desta atribuicao (AA-5/D3). Uma atribuicao DIRETA
    /// (admin do tenant concedendo a si proprio ou a terceiro a partir de poder NAO delegado) tem
    /// profundidade 0. Cada subdelegacao subsequente herda a profundidade do poder do concedente + 1.
    /// Serve para BARRAR cadeias infinitas de subdelegacao alem do limite configurado por tenant.
    /// </summary>
    public int ProfundidadeDelegacao { get; private set; }

    /// <summary>Cria uma atribuicao de papel com escopo organizacional.</summary>
    /// <param name="papelId">Papel atribuido.</param>
    /// <param name="unidadeId">UO raiz do escopo.</param>
    /// <param name="incluiSubunidades">Se inclui os descendentes da UO.</param>
    /// <param name="vigencia">Janela de vigencia.</param>
    /// <param name="origem">Procedencia da atribuicao.</param>
    /// <param name="profundidadeDelegacao">Profundidade na cadeia de subdelegacao (0 = direta — AA-5/D3).</param>
    /// <returns>Nova <see cref="AtribuicaoDePapel"/>.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="vigencia"/> ou <paramref name="origem"/> forem nulos.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="profundidadeDelegacao"/> for negativa.</exception>
    public static AtribuicaoDePapel Criar(
        PapelId papelId,
        UnidadeOrganizacionalId unidadeId,
        bool incluiSubunidades,
        Vigencia vigencia,
        OrigemAtribuicao origem,
        int profundidadeDelegacao = 0)
    {
        ArgumentNullException.ThrowIfNull(vigencia);
        ArgumentNullException.ThrowIfNull(origem);
        ArgumentOutOfRangeException.ThrowIfNegative(profundidadeDelegacao);
        return new AtribuicaoDePapel(AtribuicaoDePapelId.New(), papelId, unidadeId, incluiSubunidades, vigencia, origem, profundidadeDelegacao);
    }

    /// <summary>
    /// Indica se esta atribuicao tem o MESMO escopo de papel que outra — i.e., mesmo
    /// <see cref="PapelId"/>, mesma <see cref="UnidadeId"/> e mesmo <see cref="IncluiSubunidades"/>.
    /// Usado pelo agregado para impedir atribuicoes duplicadas (mesmo papel, mesmo escopo).
    /// </summary>
    /// <param name="papelId">Papel a comparar.</param>
    /// <param name="unidadeId">UO a comparar.</param>
    /// <param name="incluiSubunidades">Flag de subunidades a comparar.</param>
    /// <returns><c>true</c> se descrevem o mesmo escopo de papel.</returns>
    public bool MesmoEscopo(PapelId papelId, UnidadeOrganizacionalId unidadeId, bool incluiSubunidades)
        => PapelId == papelId && UnidadeId == unidadeId && IncluiSubunidades == incluiSubunidades;

    /// <summary>Indica se a atribuicao esta vigente no instante informado.</summary>
    /// <param name="instante">Momento de referencia.</param>
    /// <returns><c>true</c> se a vigencia cobre o instante.</returns>
    public bool VigenteEm(DateTimeOffset instante) => Vigencia.VigenteEm(instante);

    /// <summary>
    /// MIGRACAO DE DADOS (MODELO §10.2): re-ancora esta atribuicao na UO RAIZ real do tenant,
    /// substituindo a sentinela <see cref="UnidadeOrganizacionalId.RaizPendente"/>. Acessivel apenas
    /// pela raiz <see cref="Usuario"/> (entidade-filha; coesao do agregado preservada). Mantem o
    /// alcance (<see cref="IncluiSubunidades"/>), a vigencia e a origem — apenas a UO muda.
    /// </summary>
    /// <param name="raizId">Id da UO raiz real do tenant.</param>
    internal void ReancorarNa(UnidadeOrganizacionalId raizId) => UnidadeId = raizId;
}
