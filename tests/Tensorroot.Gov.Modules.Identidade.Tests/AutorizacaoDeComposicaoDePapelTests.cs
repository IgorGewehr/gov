using FluentAssertions;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Cobertura de dominio da regra I4 na COMPOSICAO de papel (achado AA-1) e da raiz unica da arvore.
/// "Nao delega o que nao tem": so empacota num papel quem possui a permissao de forma GLOBAL no
/// tenant (cobertura da raiz com subarvore). Deny-by-default sem raiz inequivoca.
/// </summary>
public sealed class AutorizacaoDeComposicaoDePapelTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static (ArvoreUnidades Arvore, UnidadeOrganizacionalId Raiz, UnidadeOrganizacionalId Filha) ArvoreSimples()
    {
        var raiz = UnidadeOrganizacional.CriarRaiz(Tenant, "R", "Raiz", TipoUnidade.Gabinete);
        var filha = UnidadeOrganizacional.CriarFilha(Tenant, "F", "Filha", TipoUnidade.Secretaria, raiz.Id);
        return (ArvoreUnidades.Construir([raiz, filha]), raiz.Id, filha.Id);
    }

    private static EscopoEfetivo EscopoGlobal(ArvoreUnidades arvore, UnidadeOrganizacionalId raiz, params string[] permissoes)
    {
        var papelId = PapelId.New();
        var atribuicao = AtribuicaoDePapel.Criar(
            papelId, raiz, incluiSubunidades: true,
            Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta());
        var permissoesPorPapel = new Dictionary<PapelId, IReadOnlySet<string>>
        {
            [papelId] = new HashSet<string>(permissoes, StringComparer.Ordinal),
        };
        return EscopoEfetivo.Calcular([atribuicao], permissoesPorPapel, arvore, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Raiz_unica_e_identificada()
    {
        var (arvore, raiz, _) = ArvoreSimples();

        arvore.Raiz.Should().Be(raiz);
    }

    [Fact]
    public void Raiz_ambigua_retorna_null()
    {
        var raizA = UnidadeOrganizacional.CriarRaiz(Tenant, "A", "RaizA", TipoUnidade.Gabinete);
        var raizB = UnidadeOrganizacional.CriarRaiz(Tenant, "B", "RaizB", TipoUnidade.Gabinete);

        ArvoreUnidades.Construir([raizA, raizB]).Raiz.Should().BeNull();
    }

    [Fact]
    public void Composicao_permitida_quando_possui_todas_as_permissoes_global()
    {
        var (arvore, raiz, _) = ArvoreSimples();
        var escopo = EscopoGlobal(arvore, raiz,
            DomainPermissoes.IdentidadeUsuariosGerenciar, DomainPermissoes.FinancasGerenciar);

        var resultado = AutorizacaoDeComposicaoDePapel.Verificar(
            escopo, [DomainPermissoes.FinancasGerenciar], arvore);

        resultado.Permitida.Should().BeTrue();
    }

    [Fact]
    public void Composicao_negada_quando_nao_possui_a_permissao()
    {
        var (arvore, raiz, _) = ArvoreSimples();
        var escopo = EscopoGlobal(arvore, raiz, DomainPermissoes.IdentidadeUsuariosGerenciar);

        var resultado = AutorizacaoDeComposicaoDePapel.Verificar(
            escopo, [DomainPermissoes.FinancasGerenciar], arvore);

        resultado.Permitida.Should().BeFalse();
        resultado.Motivo.Should().Be(MotivoConcessaoNegada.PermissaoNaoPossuida);
        resultado.PermissaoFaltante.Should().Be(DomainPermissoes.FinancasGerenciar);
    }

    [Fact]
    public void Composicao_negada_quando_admin_nao_cobre_subarvore_inteira()
    {
        // Possui tudo, mas SO na filha (nao na raiz): nao cobre o escopo global → nega.
        var (arvore, _, filha) = ArvoreSimples();
        var papelId = PapelId.New();
        var atribuicao = AtribuicaoDePapel.Criar(
            papelId, filha, incluiSubunidades: true,
            Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta());
        var permissoesPorPapel = new Dictionary<PapelId, IReadOnlySet<string>>
        {
            [papelId] = new HashSet<string>(
                [DomainPermissoes.IdentidadeUsuariosGerenciar, DomainPermissoes.FinancasGerenciar],
                StringComparer.Ordinal),
        };
        var escopo = EscopoEfetivo.Calcular([atribuicao], permissoesPorPapel, arvore, DateTimeOffset.UtcNow);

        var resultado = AutorizacaoDeComposicaoDePapel.Verificar(
            escopo, [DomainPermissoes.FinancasGerenciar], arvore);

        resultado.Permitida.Should().BeFalse();
        resultado.Motivo.Should().Be(MotivoConcessaoNegada.ForaDoEscopoAdministrativo);
    }

    [Fact]
    public void Composicao_sem_poder_administrativo_e_negada()
    {
        var (arvore, raiz, _) = ArvoreSimples();
        var escopo = EscopoGlobal(arvore, raiz, DomainPermissoes.FinancasGerenciar);

        var resultado = AutorizacaoDeComposicaoDePapel.Verificar(
            escopo, [DomainPermissoes.FinancasGerenciar], arvore);

        resultado.Permitida.Should().BeFalse();
        resultado.Motivo.Should().Be(MotivoConcessaoNegada.SemPoderAdministrativo);
    }

    [Fact]
    public void Papel_vazio_e_sempre_permitido()
    {
        var (arvore, _, _) = ArvoreSimples();

        AutorizacaoDeComposicaoDePapel.Verificar(EscopoEfetivo.Vazio, [], arvore)
            .Permitida.Should().BeTrue();
    }
}
