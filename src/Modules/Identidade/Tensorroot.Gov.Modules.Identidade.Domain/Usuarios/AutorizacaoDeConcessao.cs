using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

/// <summary>
/// Motivo pelo qual uma concessao (atribuicao de papel com escopo) foi NEGADA pela regra-mae I4
/// ("nao delega o que nao tem" — MODELO §3.1/§4/§8). Mensagens claras de dominio, sem vazar quais
/// permissoes especificas faltam alem do necessario para o concedente entender.
/// </summary>
public enum MotivoConcessaoNegada
{
    /// <summary>Concessao aprovada (sentinela; nao e uma negacao).</summary>
    Nenhum = 0,

    /// <summary>O concedente nao tem poder administrativo de usuarios (falta <c>identidade.usuarios.gerenciar</c>).</summary>
    SemPoderAdministrativo = 1,

    /// <summary>A UO alvo nao existe na arvore do tenant.</summary>
    UnidadeInexistente = 2,

    /// <summary>O concedente nao administra a UO alvo (escopo administrativo nao cobre X — D1/§3.1.b).</summary>
    ForaDoEscopoAdministrativo = 3,

    /// <summary>O concedente nao possui, ele proprio, todas as permissoes do papel no escopo alvo (D4/§3.1.c).</summary>
    PermissaoNaoPossuida = 4,
}

/// <summary>Resultado da verificacao de uma concessao pela regra I4.</summary>
/// <param name="Permitida">Se a concessao e permitida.</param>
/// <param name="Motivo">Motivo da negacao (<see cref="MotivoConcessaoNegada.Nenhum"/> se permitida).</param>
/// <param name="PermissaoFaltante">Permissao especifica que faltou ao concedente, quando aplicavel.</param>
public sealed record ResultadoConcessao(bool Permitida, MotivoConcessaoNegada Motivo, string? PermissaoFaltante = null)
{
    /// <summary>Resultado de concessao permitida.</summary>
    public static ResultadoConcessao Ok { get; } = new(true, MotivoConcessaoNegada.Nenhum);

    /// <summary>Cria um resultado de negacao com o motivo informado.</summary>
    /// <param name="motivo">Motivo da negacao.</param>
    /// <param name="permissaoFaltante">Permissao faltante, quando aplicavel.</param>
    /// <returns>Resultado negado.</returns>
    public static ResultadoConcessao Negar(MotivoConcessaoNegada motivo, string? permissaoFaltante = null)
        => new(false, motivo, permissaoFaltante);
}

/// <summary>
/// Servico de dominio que PROVA a regra I4 ("nao delega o que nao tem") de uma concessao —
/// MODELO §3.1 (concessao direta) e §4 (delegacao, passos D1/D4 do M1-minimo). A regra e provada
/// AQUI, no dominio, e nao apenas na UI: atribuir um papel P a um usuario numa UO X exige do
/// concedente (a) poder administrativo de usuarios; (b) escopo administrativo sobre X; (c) possuir,
/// ele proprio, TODAS as permissoes de P em X ou em um ANCESTRAL de X.
/// </summary>
/// <remarks>
/// "Possuir em X ou ancestral" e expresso expandindo o escopo alvo para {X} (sem subunidades) ou
/// para a subarvore de X (com subunidades) e exigindo que, para CADA permissao de P, o conjunto de
/// UOs do concedente seja superconjunto do escopo alvo — onde o escopo do concedente ja inclui os
/// descendentes (autoridade flui para baixo). A clausula "ancestral" e respeitada porque uma
/// atribuicao do concedente num ancestral de X com <c>IncluiSubunidades=true</c> ja contem X (e sua
/// subarvore) apos a expansao. ADIADO p/ M1.x: D3 (subconjunto de PermissoesDelegaveis da
/// ConcessaoDelegacao), D5 (clearance/sensibilidade) e D6 alem da vigencia ja considerada no escopo.
/// </remarks>
public static class AutorizacaoDeConcessao
{
    /// <summary>
    /// Verifica se um concedente, com o escopo efetivo informado, pode atribuir o papel (com as
    /// permissoes dadas) a um usuario na UO alvo com o alcance de subunidades indicado.
    /// </summary>
    /// <param name="escopoConcedente">Escopo efetivo (permissao -> UOs) do concedente.</param>
    /// <param name="permissoesDoPapel">Permissoes do papel a ser concedido.</param>
    /// <param name="unidadeAlvo">UO em que o papel sera atribuido.</param>
    /// <param name="incluiSubunidades">Se a atribuicao alcanca os descendentes da UO alvo.</param>
    /// <param name="arvore">Arvore de UOs do tenant.</param>
    /// <returns>Resultado da verificacao (permitida ou motivo da negacao).</returns>
    /// <exception cref="ArgumentNullException">Se algum argumento for nulo.</exception>
    public static ResultadoConcessao Verificar(
        EscopoEfetivo escopoConcedente,
        IReadOnlySet<string> permissoesDoPapel,
        UnidadeOrganizacionalId unidadeAlvo,
        bool incluiSubunidades,
        ArvoreUnidades arvore)
    {
        ArgumentNullException.ThrowIfNull(escopoConcedente);
        ArgumentNullException.ThrowIfNull(permissoesDoPapel);
        ArgumentNullException.ThrowIfNull(arvore);

        // (a) Poder administrativo de usuarios (§3.1.a) — pre-requisito de qualquer concessao.
        if (!escopoConcedente.Possui(DomainPermissoes.IdentidadeUsuariosGerenciar))
        {
            return ResultadoConcessao.Negar(MotivoConcessaoNegada.SemPoderAdministrativo);
        }

        // A UO alvo deve existir na arvore do tenant.
        if (!arvore.Contem(unidadeAlvo))
        {
            return ResultadoConcessao.Negar(MotivoConcessaoNegada.UnidadeInexistente);
        }

        // Escopo alvo concreto: a UO (e, se aplicavel, sua subarvore).
        var escopoAlvo = arvore.Expandir(unidadeAlvo, incluiSubunidades);

        // (b) Escopo administrativo sobre X (§3.1.b/D1): o concedente precisa administrar todo o
        // escopo alvo — i.e., possuir 'identidade.usuarios.gerenciar' cobrindo {X (+ subarvore)}.
        if (!escopoConcedente.CobreEscopo(DomainPermissoes.IdentidadeUsuariosGerenciar, escopoAlvo))
        {
            return ResultadoConcessao.Negar(MotivoConcessaoNegada.ForaDoEscopoAdministrativo);
        }

        // (c) "Nao delega o que nao tem" (§3.1.c/D4): para CADA permissao do papel, o concedente
        // deve possui-la cobrindo todo o escopo alvo (em X ou herdada de um ancestral via expansao).
        foreach (var permissao in permissoesDoPapel)
        {
            if (!escopoConcedente.CobreEscopo(permissao, escopoAlvo))
            {
                return ResultadoConcessao.Negar(MotivoConcessaoNegada.PermissaoNaoPossuida, permissao);
            }
        }

        return ResultadoConcessao.Ok;
    }
}
