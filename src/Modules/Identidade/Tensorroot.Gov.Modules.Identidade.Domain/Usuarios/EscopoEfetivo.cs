using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

/// <summary>
/// Escopo efetivo de um sujeito: para cada PERMISSAO do catalogo, o conjunto de Unidades
/// Organizacionais (UOs) em que ele a possui. E o resultado de cruzar as
/// <see cref="AtribuicaoDePapel"/> VIGENTES do usuario com as permissoes dos seus papeis e expandir
/// cada escopo (<see cref="AtribuicaoDePapel.IncluiSubunidades"/>) na arvore (invariante I5).
/// Substitui o antigo "set plano de permissoes" pela visao rica (permissao -> conjunto de UOs)
/// exigida pelo enforcement por UO (MODELO §3, §5, §10.3).
/// </summary>
/// <remarks>
/// Objeto de leitura imutavel, calculado por <see cref="Calcular"/> no dominio (sem I/O). A borda
/// HTTP continua usando a projecao PLANA <see cref="Permissoes"/> (uma claim "perm" por permissao);
/// o filtro fino por UO usa <see cref="UnidadesDaPermissao"/>/<see cref="CobreEscopo"/>.
/// </remarks>
public sealed class EscopoEfetivo
{
    private readonly IReadOnlyDictionary<string, IReadOnlySet<UnidadeOrganizacionalId>> _unidadesPorPermissao;

    private EscopoEfetivo(IReadOnlyDictionary<string, IReadOnlySet<UnidadeOrganizacionalId>> unidadesPorPermissao)
        => _unidadesPorPermissao = unidadesPorPermissao;

    /// <summary>Escopo vazio (sujeito sem nenhuma permissao em nenhuma UO) — deny-by-default (I2).</summary>
    public static EscopoEfetivo Vazio { get; } =
        new(new Dictionary<string, IReadOnlySet<UnidadeOrganizacionalId>>(StringComparer.Ordinal));

    /// <summary>Permissoes efetivas (projecao PLANA, distinta e ordenada) — para as claims do token.</summary>
    public IReadOnlyList<string> Permissoes
    {
        get
        {
            var ordenadas = new SortedSet<string>(_unidadesPorPermissao.Keys, StringComparer.Ordinal);
            return ordenadas.ToArray();
        }
    }

    /// <summary>
    /// Calcula o escopo efetivo de um usuario num instante: para cada atribuicao VIGENTE, resolve as
    /// permissoes do papel correspondente e as associa ao conjunto de UOs alcancadas pelo escopo da
    /// atribuicao (expandindo subunidades na arvore). A uniao por permissao acumula as UOs de todas
    /// as atribuicoes que a concedem.
    /// </summary>
    /// <param name="atribuicoes">Atribuicoes de papel do usuario (com escopo, vigencia).</param>
    /// <param name="permissoesPorPapel">Permissoes de cada papel referenciado pelas atribuicoes.</param>
    /// <param name="arvore">Arvore de UOs do tenant (para expandir subunidades).</param>
    /// <param name="instante">Momento de referencia para a vigencia.</param>
    /// <returns>Escopo efetivo (permissao -> conjunto de UOs).</returns>
    /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
    public static EscopoEfetivo Calcular(
        IEnumerable<AtribuicaoDePapel> atribuicoes,
        IReadOnlyDictionary<PapelId, IReadOnlySet<string>> permissoesPorPapel,
        ArvoreUnidades arvore,
        DateTimeOffset instante)
    {
        ArgumentNullException.ThrowIfNull(atribuicoes);
        ArgumentNullException.ThrowIfNull(permissoesPorPapel);
        ArgumentNullException.ThrowIfNull(arvore);

        var acumulado = new Dictionary<string, HashSet<UnidadeOrganizacionalId>>(StringComparer.Ordinal);

        foreach (var atribuicao in atribuicoes)
        {
            if (!atribuicao.VigenteEm(instante))
            {
                continue;
            }

            if (!permissoesPorPapel.TryGetValue(atribuicao.PapelId, out var permissoes))
            {
                continue;
            }

            var alcancadas = arvore.Expandir(atribuicao.UnidadeId, atribuicao.IncluiSubunidades);
            foreach (var permissao in permissoes)
            {
                if (!acumulado.TryGetValue(permissao, out var uos))
                {
                    uos = [];
                    acumulado[permissao] = uos;
                }

                uos.UnionWith(alcancadas);
            }
        }

        var congelado = acumulado.ToDictionary(
            par => par.Key,
            par => (IReadOnlySet<UnidadeOrganizacionalId>)par.Value,
            StringComparer.Ordinal);

        return new EscopoEfetivo(congelado);
    }

    /// <summary>Conjunto de UOs em que o sujeito possui a permissao informada (vazio se nao a possui).</summary>
    /// <param name="permissao">Escopo do catalogo canonico.</param>
    /// <returns>UOs em que a permissao vale.</returns>
    public IReadOnlySet<UnidadeOrganizacionalId> UnidadesDaPermissao(string permissao)
        => _unidadesPorPermissao.TryGetValue(permissao, out var uos)
            ? uos
            : EmptySet;

    /// <summary>Indica se o sujeito possui a permissao em ALGUMA UO (RBAC grosso, sem escopo).</summary>
    /// <param name="permissao">Escopo do catalogo canonico.</param>
    /// <returns><c>true</c> se possui em ao menos uma UO.</returns>
    public bool Possui(string permissao) => _unidadesPorPermissao.ContainsKey(permissao);

    /// <summary>
    /// Indica se o sujeito COBRE o escopo (permissao + conjunto de UOs alvo): possui a permissao e
    /// o seu conjunto de UOs para ela e SUPERCONJUNTO das UOs alvo. Base da regra I4 (D4) — o
    /// concedente "tem" tudo o que pretende conceder no escopo pedido.
    /// </summary>
    /// <param name="permissao">Permissao requerida.</param>
    /// <param name="unidadesAlvo">UOs que o escopo alvo abrange.</param>
    /// <returns><c>true</c> se o sujeito cobre integralmente o escopo.</returns>
    public bool CobreEscopo(string permissao, IReadOnlySet<UnidadeOrganizacionalId> unidadesAlvo)
    {
        ArgumentNullException.ThrowIfNull(unidadesAlvo);
        return _unidadesPorPermissao.TryGetValue(permissao, out var minhas) && minhas.IsSupersetOf(unidadesAlvo);
    }

    private static readonly IReadOnlySet<UnidadeOrganizacionalId> EmptySet =
        new HashSet<UnidadeOrganizacionalId>();
}
