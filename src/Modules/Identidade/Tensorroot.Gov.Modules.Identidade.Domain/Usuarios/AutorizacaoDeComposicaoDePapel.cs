using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

/// <summary>
/// Servico de dominio que PROVA a regra I4 ("nao delega o que nao tem" — MODELO §3.1.c/§8) na
/// COMPOSICAO de um papel (RBAC): empacotar uma permissao P num papel e, em efeito pratico, prepara
/// a delegacao de P a quem receber o papel. Logo, o compositor (usuario atual) so pode incluir P num
/// papel se ele PROPRIO possui P de forma EFETIVAMENTE GLOBAL no tenant — i.e., cobrindo a UO raiz
/// COM subarvore (a mesma ancora "global" que <see cref="Usuario.DefinirPapeis"/> e o seed usam).
/// </summary>
/// <remarks>
/// Diferentemente de <see cref="AutorizacaoDeConcessao"/> (que prova cobertura num escopo de UO
/// especifico, ja que a atribuicao carrega UO), o PAPEL nao tem UO: suas permissoes podem vir a ser
/// atribuidas em QUALQUER UO do tenant (inclusive a raiz, via PUT /papeis ou pela ponte de
/// compatibilidade). Por isso, a barra correta para compor o papel e a posse GLOBAL: o compositor
/// cobre {raiz + toda a subarvore}. Sem isso, quem tem apenas <c>identidade.usuarios.gerenciar</c>
/// empacotaria permissoes fiscais/saude/TCE que nao possui (escalonamento — achado AA-1 do RED-TEAM).
/// </remarks>
public static class AutorizacaoDeComposicaoDePapel
{
    /// <summary>
    /// Verifica se o compositor, com o escopo efetivo informado, pode incluir num papel o conjunto de
    /// permissoes desejado. Exige (a) poder administrativo de usuarios global e (b) posse GLOBAL de
    /// CADA permissao do papel (cobertura da raiz com subarvore). A primeira permissao nao coberta e
    /// reportada (deny-by-default, nunca silencioso).
    /// </summary>
    /// <param name="escopoCompositor">Escopo efetivo (permissao -> UOs) do compositor.</param>
    /// <param name="permissoesDoPapel">Conjunto de permissoes a empacotar no papel.</param>
    /// <param name="arvore">Arvore de UOs do tenant.</param>
    /// <returns>Resultado da verificacao (permitida ou motivo da negacao).</returns>
    /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
    public static ResultadoConcessao Verificar(
        EscopoEfetivo escopoCompositor,
        IReadOnlyCollection<string> permissoesDoPapel,
        ArvoreUnidades arvore)
    {
        ArgumentNullException.ThrowIfNull(escopoCompositor);
        ArgumentNullException.ThrowIfNull(permissoesDoPapel);
        ArgumentNullException.ThrowIfNull(arvore);

        // Papel vazio nao concede nada: nada a delegar, nada a provar.
        if (permissoesDoPapel.Count == 0)
        {
            return ResultadoConcessao.Ok;
        }

        // (a) Poder administrativo de usuarios (§3.1.a) — pre-requisito de qualquer composicao RBAC.
        if (!escopoCompositor.Possui(DomainPermissoes.IdentidadeUsuariosGerenciar))
        {
            return ResultadoConcessao.Negar(MotivoConcessaoNegada.SemPoderAdministrativo);
        }

        // Escopo GLOBAL do tenant: a raiz com toda a sua subarvore. O papel nao carrega UO, entao a
        // posse exigida e a mais abrangente possivel (cobre qualquer UO em que o papel venha a ser
        // atribuido). Sem raiz identificavel, nega por padrao (nao ha como provar cobertura global).
        var raiz = arvore.Raiz;
        if (raiz is not { } raizId)
        {
            return ResultadoConcessao.Negar(MotivoConcessaoNegada.ForaDoEscopoAdministrativo);
        }

        var escopoGlobal = arvore.Expandir(raizId, incluiSubunidades: true);

        // (b) Poder administrativo cobrindo o tenant inteiro (§3.1.b/D1).
        if (!escopoCompositor.CobreEscopo(DomainPermissoes.IdentidadeUsuariosGerenciar, escopoGlobal))
        {
            return ResultadoConcessao.Negar(MotivoConcessaoNegada.ForaDoEscopoAdministrativo);
        }

        // (c) "Nao delega o que nao tem" (§3.1.c/D4): para CADA permissao do papel, o compositor deve
        // possui-la cobrindo o tenant inteiro.
        foreach (var permissao in permissoesDoPapel)
        {
            if (!escopoCompositor.CobreEscopo(permissao, escopoGlobal))
            {
                return ResultadoConcessao.Negar(MotivoConcessaoNegada.PermissaoNaoPossuida, permissao);
            }
        }

        return ResultadoConcessao.Ok;
    }
}
