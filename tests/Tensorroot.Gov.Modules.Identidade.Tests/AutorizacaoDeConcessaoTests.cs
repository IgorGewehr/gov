using FluentAssertions;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Prova, NO DOMINIO, a regra-mae I4 ("nao delega o que nao tem" — MODELO §3.1/§4/§8) e a expansao
/// de escopo de UO (I5): o concedente so atribui o papel P na UO X se possui poder administrativo
/// sobre X E todas as permissoes de P em X (ou herdadas de um ancestral). Cobre tambem a visao rica
/// (permissao -> conjunto de UOs) do <see cref="EscopoEfetivo"/>.
/// </summary>
public sealed class AutorizacaoDeConcessaoTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Arvore: PREF (raiz) -> SMS (saude) -> UBS (subunidade de SMS); e EDU (educacao) sob PREF.
    private readonly UnidadeOrganizacional _pref = UnidadeOrganizacional.CriarRaiz(Tenant, "PREF", "Prefeitura", TipoUnidade.Gabinete);
    private readonly UnidadeOrganizacional _sms;
    private readonly UnidadeOrganizacional _ubs;
    private readonly UnidadeOrganizacional _edu;
    private readonly ArvoreUnidades _arvore;

    public AutorizacaoDeConcessaoTests()
    {
        _sms = UnidadeOrganizacional.CriarFilha(Tenant, "SMS", "Saude", TipoUnidade.Secretaria, _pref.Id);
        _ubs = UnidadeOrganizacional.CriarFilha(Tenant, "SMS.UBS", "UBS Central", TipoUnidade.Setor, _sms.Id);
        _edu = UnidadeOrganizacional.CriarFilha(Tenant, "EDU", "Educacao", TipoUnidade.Secretaria, _pref.Id);
        _arvore = ArvoreUnidades.Construir([_pref, _sms, _ubs, _edu]);
    }

    // Escopo efetivo de um concedente que detem 'identidade.usuarios.gerenciar' + 'saude.gerenciar'
    // na UO informada COM subunidades.
    private EscopoEfetivo EscopoComSaudeEAdmin(UnidadeOrganizacionalId uo, bool incluiSub = true)
    {
        var papelId = PapelId.New();
        var atribuicao = AtribuicaoDePapel.Criar(
            papelId, uo, incluiSub, Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta());

        var permissoes = new Dictionary<PapelId, IReadOnlySet<string>>
        {
            [papelId] = new HashSet<string>(
                [DomainPermissoes.IdentidadeUsuariosGerenciar, DomainPermissoes.SaudeGerenciar, DomainPermissoes.SaudeVer],
                StringComparer.Ordinal),
        };

        return EscopoEfetivo.Calcular([atribuicao], permissoes, _arvore, DateTimeOffset.UtcNow);
    }

    private static HashSet<string> PermissoesDoPapel(params string[] permissoes)
        => new(permissoes, StringComparer.Ordinal);

    [Fact]
    public void Concedente_que_administra_SMS_pode_atribuir_papel_de_saude_na_UBS_subunidade()
    {
        // Concedente administra SMS COM subunidades → cobre a UBS (descendente). Papel = saude.ver.
        var escopo = EscopoComSaudeEAdmin(_sms.Id);

        var resultado = AutorizacaoDeConcessao.Verificar(
            escopo, PermissoesDoPapel(DomainPermissoes.SaudeVer), _ubs.Id, incluiSubunidades: false, _arvore);

        resultado.Permitida.Should().BeTrue();
        resultado.Motivo.Should().Be(MotivoConcessaoNegada.Nenhum);
    }

    [Fact]
    public void Concedente_sem_poder_administrativo_e_negado()
    {
        var papelId = PapelId.New();
        var atribuicao = AtribuicaoDePapel.Criar(
            papelId, _sms.Id, incluiSubunidades: true, Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta());
        var permissoes = new Dictionary<PapelId, IReadOnlySet<string>>
        {
            [papelId] = PermissoesDoPapel(DomainPermissoes.SaudeGerenciar), // sem identidade.usuarios.gerenciar
        };
        var escopo = EscopoEfetivo.Calcular([atribuicao], permissoes, _arvore, DateTimeOffset.UtcNow);

        var resultado = AutorizacaoDeConcessao.Verificar(
            escopo, PermissoesDoPapel(DomainPermissoes.SaudeVer), _sms.Id, incluiSubunidades: false, _arvore);

        resultado.Permitida.Should().BeFalse();
        resultado.Motivo.Should().Be(MotivoConcessaoNegada.SemPoderAdministrativo);
    }

    [Fact]
    public void Nao_delega_o_que_nao_tem_negado_quando_falta_permissao_do_papel()
    {
        // Concedente so tem saude.* + admin em SMS; tenta conceder um papel com legislativo.gerenciar.
        var escopo = EscopoComSaudeEAdmin(_sms.Id);

        var resultado = AutorizacaoDeConcessao.Verificar(
            escopo,
            PermissoesDoPapel(DomainPermissoes.SaudeVer, DomainPermissoes.LegislativoGerenciar),
            _sms.Id,
            incluiSubunidades: false,
            _arvore);

        resultado.Permitida.Should().BeFalse();
        resultado.Motivo.Should().Be(MotivoConcessaoNegada.PermissaoNaoPossuida);
        resultado.PermissaoFaltante.Should().Be(DomainPermissoes.LegislativoGerenciar);
    }

    [Fact]
    public void Fora_do_escopo_administrativo_negado_quando_alvo_e_outra_secretaria()
    {
        // Concedente administra SMS (e descendentes), mas tenta atribuir na EDU (irma, fora do escopo).
        var escopo = EscopoComSaudeEAdmin(_sms.Id);

        var resultado = AutorizacaoDeConcessao.Verificar(
            escopo, PermissoesDoPapel(DomainPermissoes.SaudeVer), _edu.Id, incluiSubunidades: false, _arvore);

        resultado.Permitida.Should().BeFalse();
        resultado.Motivo.Should().Be(MotivoConcessaoNegada.ForaDoEscopoAdministrativo);
    }

    [Fact]
    public void Escopo_sem_subunidades_nao_cobre_a_subarvore_negando_concessao_com_subunidades_na_filha()
    {
        // Concedente tem admin+saude APENAS em SMS (sem subunidades) → nao cobre {UBS}.
        var escopo = EscopoComSaudeEAdmin(_sms.Id, incluiSub: false);

        var resultado = AutorizacaoDeConcessao.Verificar(
            escopo, PermissoesDoPapel(DomainPermissoes.SaudeVer), _ubs.Id, incluiSubunidades: false, _arvore);

        resultado.Permitida.Should().BeFalse();
        resultado.Motivo.Should().Be(MotivoConcessaoNegada.ForaDoEscopoAdministrativo);
    }

    [Fact]
    public void EscopoEfetivo_expande_subunidades_na_visao_permissao_para_conjunto_de_UOs()
    {
        // Admin global na raiz com subunidades → todas as permissoes valem em TODAS as UOs.
        var escopo = EscopoComSaudeEAdmin(_pref.Id);

        escopo.UnidadesDaPermissao(DomainPermissoes.SaudeVer)
            .Should().BeEquivalentTo(new[] { _pref.Id, _sms.Id, _ubs.Id, _edu.Id });
        escopo.Possui(DomainPermissoes.SaudeGerenciar).Should().BeTrue();
        escopo.Permissoes.Should().Contain(DomainPermissoes.IdentidadeUsuariosGerenciar);
    }

    [Fact]
    public void EscopoEfetivo_ignora_atribuicao_vencida()
    {
        var papelId = PapelId.New();
        var ontem = DateTimeOffset.UtcNow.AddDays(-2);
        var vencida = AtribuicaoDePapel.Criar(
            papelId, _sms.Id, incluiSubunidades: true, Vigencia.Criar(ontem, ontem.AddDays(1)), OrigemAtribuicao.Direta());
        var permissoes = new Dictionary<PapelId, IReadOnlySet<string>>
        {
            [papelId] = PermissoesDoPapel(DomainPermissoes.SaudeVer),
        };

        var escopo = EscopoEfetivo.Calcular([vencida], permissoes, _arvore, DateTimeOffset.UtcNow);

        escopo.Possui(DomainPermissoes.SaudeVer).Should().BeFalse();
    }
}
