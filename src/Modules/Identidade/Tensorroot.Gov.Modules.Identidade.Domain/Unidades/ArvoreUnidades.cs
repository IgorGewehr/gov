using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

/// <summary>
/// Servico de dominio (puro, sem I/O) que materializa a arvore de Unidades Organizacionais de um
/// tenant para responder as perguntas estruturais de autorizacao: quem sao os descendentes de uma
/// UO (expansao de <c>IncluiSubunidades</c> — invariante I5) e qual a cadeia de ancestrais de uma
/// UO (usada pela regra I4: "no escopo X OU em um ancestral"). A arvore e construida a partir do
/// conjunto de UOs do tenant (resolvido pela aplicacao/persistencia); o servico nao navega o banco.
/// </summary>
/// <remarks>
/// MODELO §3.1/§4/§5/§8: o escopo de uma <see cref="AtribuicaoDePapel"/> e a subarvore enraizada em
/// <see cref="AtribuicaoDePapel.UnidadeId"/> quando <see cref="AtribuicaoDePapel.IncluiSubunidades"/>
/// e verdadeiro; caso contrario, apenas a UO exata. A regra "nao delega o que nao tem" (I4) exige
/// que o concedente possua a permissao na UO alvo OU em um de seus ANCESTRAIS — por isso a arvore
/// precisa responder tanto descendentes (expansao do escopo do concedente) quanto ancestrais (da UO
/// alvo). UOs inativas continuam na arvore (preservam a topologia/historico); a decisao de barrar
/// atribuicao em UO inativa e da aplicacao.
/// </remarks>
public sealed class ArvoreUnidades
{
    private readonly Dictionary<UnidadeOrganizacionalId, UnidadeOrganizacionalId?> _paiPorId;
    private readonly Dictionary<UnidadeOrganizacionalId, List<UnidadeOrganizacionalId>> _filhosPorId;

    private ArvoreUnidades(
        Dictionary<UnidadeOrganizacionalId, UnidadeOrganizacionalId?> paiPorId,
        Dictionary<UnidadeOrganizacionalId, List<UnidadeOrganizacionalId>> filhosPorId)
    {
        _paiPorId = paiPorId;
        _filhosPorId = filhosPorId;
    }

    /// <summary>
    /// Constroi a arvore a partir do conjunto de UOs do tenant. Cada UO contribui com seu par
    /// (id, paiId); ids referenciados como pai mas ausentes do conjunto sao tratados como raizes
    /// (defensivo). Nao valida ciclos (a invariante I5 e garantida na escrita via
    /// <see cref="UnidadeOrganizacional.DefinirPai"/>).
    /// </summary>
    /// <param name="unidades">UOs do tenant.</param>
    /// <returns>Arvore pronta para consulta.</returns>
    /// <exception cref="ArgumentNullException">Se a colecao for nula.</exception>
    public static ArvoreUnidades Construir(IEnumerable<UnidadeOrganizacional> unidades)
    {
        ArgumentNullException.ThrowIfNull(unidades);

        var paiPorId = new Dictionary<UnidadeOrganizacionalId, UnidadeOrganizacionalId?>();
        var filhosPorId = new Dictionary<UnidadeOrganizacionalId, List<UnidadeOrganizacionalId>>();

        foreach (var unidade in unidades)
        {
            paiPorId[unidade.Id] = unidade.UnidadePaiId;
            if (!filhosPorId.ContainsKey(unidade.Id))
            {
                filhosPorId[unidade.Id] = [];
            }

            if (unidade.UnidadePaiId is { } paiId)
            {
                if (!filhosPorId.TryGetValue(paiId, out var filhos))
                {
                    filhos = [];
                    filhosPorId[paiId] = filhos;
                }

                filhos.Add(unidade.Id);
            }
        }

        return new ArvoreUnidades(paiPorId, filhosPorId);
    }

    /// <summary>Indica se a UO informada existe na arvore do tenant.</summary>
    /// <param name="unidadeId">UO a verificar.</param>
    /// <returns><c>true</c> se conhecida.</returns>
    public bool Contem(UnidadeOrganizacionalId unidadeId) => _paiPorId.ContainsKey(unidadeId);

    /// <summary>
    /// Expande um escopo de atribuicao em o CONJUNTO de UOs efetivamente alcancadas: apenas a UO
    /// quando <paramref name="incluiSubunidades"/> e falso; a UO mais todos os descendentes quando
    /// verdadeiro (invariante I5 — subarvore conexa).
    /// </summary>
    /// <param name="unidadeId">UO raiz do escopo.</param>
    /// <param name="incluiSubunidades">Se inclui os descendentes.</param>
    /// <returns>Conjunto de UOs alcancadas (inclui sempre a propria UO).</returns>
    public IReadOnlySet<UnidadeOrganizacionalId> Expandir(UnidadeOrganizacionalId unidadeId, bool incluiSubunidades)
    {
        var alcancadas = new HashSet<UnidadeOrganizacionalId> { unidadeId };
        if (!incluiSubunidades)
        {
            return alcancadas;
        }

        var pilha = new Stack<UnidadeOrganizacionalId>();
        pilha.Push(unidadeId);
        while (pilha.Count > 0)
        {
            var atual = pilha.Pop();
            if (!_filhosPorId.TryGetValue(atual, out var filhos))
            {
                continue;
            }

            foreach (var filho in filhos)
            {
                if (alcancadas.Add(filho))
                {
                    pilha.Push(filho);
                }
            }
        }

        return alcancadas;
    }

    /// <summary>
    /// Devolve a UO informada e a cadeia de seus ANCESTRAIS (a propria UO, seu pai, avo, ... ate a
    /// raiz). Usada pela regra I4: o concedente cobre o escopo X se possui a permissao em X ou em
    /// qualquer ancestral de X (a autoridade flui de cima para baixo na arvore).
    /// </summary>
    /// <param name="unidadeId">UO alvo.</param>
    /// <returns>UO alvo seguida de seus ancestrais ate a raiz.</returns>
    public IReadOnlySet<UnidadeOrganizacionalId> ComAncestrais(UnidadeOrganizacionalId unidadeId)
    {
        var cadeia = new HashSet<UnidadeOrganizacionalId>();
        var atual = (UnidadeOrganizacionalId?)unidadeId;
        while (atual is { } id && cadeia.Add(id))
        {
            atual = _paiPorId.TryGetValue(id, out var pai) ? pai : null;
        }

        return cadeia;
    }
}
