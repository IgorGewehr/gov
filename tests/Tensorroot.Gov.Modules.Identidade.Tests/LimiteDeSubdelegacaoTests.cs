using FluentAssertions;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// AA-5/D3 (RED-TEAM): a delegação NÃO pode ter profundidade ilimitada. Ao conceder papéis/escopo,
/// a cadeia de subdelegação é barrada por um teto PARAMETRIZÁVEL por tenant
/// (<see cref="PoliticaDelegacao"/>), contendo a propagação lateral (amplificador de AA-1/AA-2).
/// Prova: (a) auto-atribuição direta = profundidade 0 e não se sujeita ao teto; (b) delegação herda
/// profundidade do concedente + 1; (c) ultrapassar o teto é NEGADO; (d) o teto é configurável.
/// </summary>
public sealed class LimiteDeSubdelegacaoTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly UnidadeOrganizacional _pref = UnidadeOrganizacional.CriarRaiz(Tenant, "PREF", "Prefeitura", TipoUnidade.Gabinete);
    private readonly ArvoreUnidades _arvore;

    public LimiteDeSubdelegacaoTests() => _arvore = ArvoreUnidades.Construir([_pref]);

    // Escopo de um concedente que detem 'identidade.usuarios.gerenciar' + 'saude.ver' na raiz (com
    // subunidades) COM uma dada profundidade de delegacao do PODER administrativo.
    private EscopoEfetivo EscopoAdmin(int profundidade)
    {
        var papelId = PapelId.New();
        var atribuicao = AtribuicaoDePapel.Criar(
            papelId, _pref.Id, incluiSubunidades: true,
            Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta(), profundidade);

        var permissoes = new Dictionary<PapelId, IReadOnlySet<string>>
        {
            [papelId] = new HashSet<string>(
                [DomainPermissoes.IdentidadeUsuariosGerenciar, DomainPermissoes.SaudeVer], StringComparer.Ordinal),
        };

        return EscopoEfetivo.Calcular([atribuicao], permissoes, _arvore, DateTimeOffset.UtcNow);
    }

    private static HashSet<string> Papel(params string[] permissoes) => new(permissoes, StringComparer.Ordinal);

    [Fact]
    public void Atribuicao_direta_auto_administracao_tem_profundidade_zero_e_nao_se_sujeita_ao_teto()
    {
        // Concedente == alvo (ehDelegacao=false): direta, profundidade 0, mesmo com teto 0.
        var escopo = EscopoAdmin(profundidade: 5);

        var resultado = AutorizacaoDeConcessao.Verificar(
            escopo, Papel(DomainPermissoes.SaudeVer), _pref.Id, incluiSubunidades: false, _arvore,
            ehDelegacao: false, profundidadeDoConcedente: 5, PoliticaDelegacao.Com(0));

        resultado.Permitida.Should().BeTrue();
        resultado.ProfundidadeResultante.Should().Be(0);
    }

    [Fact]
    public void Delegacao_herda_profundidade_do_concedente_mais_um()
    {
        // Concedente com poder de profundidade 0 delega a um terceiro → nova atribuicao profundidade 1.
        var escopo = EscopoAdmin(profundidade: 0);

        var resultado = AutorizacaoDeConcessao.Verificar(
            escopo, Papel(DomainPermissoes.SaudeVer), _pref.Id, incluiSubunidades: false, _arvore,
            ehDelegacao: true, profundidadeDoConcedente: 0, PoliticaDelegacao.Com(2));

        resultado.Permitida.Should().BeTrue();
        resultado.ProfundidadeResultante.Should().Be(1);
    }

    [Fact]
    public void Delegacao_que_excede_o_teto_e_negada()
    {
        // Poder do concedente ja esta na profundidade 2; delegar criaria 3 > teto 2 → NEGA.
        var escopo = EscopoAdmin(profundidade: 2);

        var resultado = AutorizacaoDeConcessao.Verificar(
            escopo, Papel(DomainPermissoes.SaudeVer), _pref.Id, incluiSubunidades: false, _arvore,
            ehDelegacao: true, profundidadeDoConcedente: 2, PoliticaDelegacao.Com(2));

        resultado.Permitida.Should().BeFalse();
        resultado.Motivo.Should().Be(MotivoConcessaoNegada.LimiteDeProfundidadeExcedido);
    }

    [Fact]
    public void Teto_zero_proibe_qualquer_subdelegacao_mas_permite_atribuicao_direta()
    {
        var escopo = EscopoAdmin(profundidade: 0);

        // Teto 0: o admin direto NÃO pode delegar a terceiros (qualquer delegação viraria profundidade 1).
        var delegacao = AutorizacaoDeConcessao.Verificar(
            escopo, Papel(DomainPermissoes.SaudeVer), _pref.Id, incluiSubunidades: false, _arvore,
            ehDelegacao: true, profundidadeDoConcedente: 0, PoliticaDelegacao.Com(0));

        delegacao.Permitida.Should().BeFalse();
        delegacao.Motivo.Should().Be(MotivoConcessaoNegada.LimiteDeProfundidadeExcedido);
    }

    [Fact]
    public void EscopoEfetivo_expoe_a_maior_profundidade_de_delegacao_da_permissao()
    {
        // Duas atribuicoes da MESMA permissao com profundidades distintas: vale a MAIOR.
        var p1 = PapelId.New();
        var p2 = PapelId.New();
        var a1 = AtribuicaoDePapel.Criar(p1, _pref.Id, true, Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta(), 1);
        var a2 = AtribuicaoDePapel.Criar(p2, _pref.Id, true, Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta(), 3);
        var permissoes = new Dictionary<PapelId, IReadOnlySet<string>>
        {
            [p1] = Papel(DomainPermissoes.SaudeVer),
            [p2] = Papel(DomainPermissoes.SaudeVer),
        };

        var escopo = EscopoEfetivo.Calcular([a1, a2], permissoes, _arvore, DateTimeOffset.UtcNow);

        escopo.ProfundidadeDeDelegacao(DomainPermissoes.SaudeVer).Should().Be(3);
        escopo.ProfundidadeDeDelegacao(DomainPermissoes.IdentidadeUsuariosGerenciar).Should().Be(0);
    }

    [Fact]
    public void Politica_padrao_e_conservadora()
        => PoliticaDelegacao.Padrao.ProfundidadeMaxima.Should().Be(PoliticaDelegacao.ProfundidadeMaximaPadrao);
}
