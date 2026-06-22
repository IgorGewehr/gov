using FluentAssertions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Papeis;
using Tensorroot.Gov.Modules.Identidade.Application.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// SEGURANCA CRITICA — fecha a cadeia de autoescalonamento dos achados AA-1 (composicao de papel sem
/// prova I4) e AA-2 (PUT /papeis atribui em escopo global sem I4) do RED-TEAM. Prova que os caminhos
/// administrativos que contornavam <c>POST /atribuicoes</c> agora exigem a MESMA cobertura I4 ("nao
/// delega o que nao tem"): um admin que so possui <c>identidade.usuarios.gerenciar</c> NAO consegue
/// empacotar nem auto-atribuir permissoes que nao possui.
/// </summary>
public sealed class ComposicaoEAtribuicaoI4Tests
{
    private const string Hash = "$2a$04$hashqualquerparaoteste0000000000000000000000000000";
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // === AA-1: composicao de papel (CriarPapel + DefinirPermissoesDoPapel) ===

    [Fact]
    public async Task DefinirPermissoes_admin_limitado_nao_empacota_permissao_que_nao_possui()
    {
        var cenario = Cenario.ComAdminLimitado();
        var papel = cenario.AdicionarPapel("Operacional");
        var handler = cenario.DefinirPermissoesHandler();

        var acao = async () => await handler.Handle(
            new DefinirPermissoesDoPapelCommand(papel.Id.Value, [DomainPermissoes.FinancasGerenciar]),
            CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        excecao.Which.Motivo.Should().Be(MotivoConcessaoNegada.PermissaoNaoPossuida);
        excecao.Which.PermissaoFaltante.Should().Be(DomainPermissoes.FinancasGerenciar);
        papel.Permissoes.Should().BeEmpty();
        cenario.Salvou.Should().BeFalse();
    }

    [Fact]
    public async Task DefinirPermissoes_admin_pleno_empacota_permissao_que_possui()
    {
        var cenario = Cenario.ComAdminPleno();
        var papel = cenario.AdicionarPapel("Tesouraria");
        var handler = cenario.DefinirPermissoesHandler();

        await handler.Handle(
            new DefinirPermissoesDoPapelCommand(papel.Id.Value, [DomainPermissoes.FinancasGerenciar]),
            CancellationToken.None);

        papel.Permissoes.Should().Contain(DomainPermissoes.FinancasGerenciar);
        cenario.Salvou.Should().BeTrue();
    }

    [Fact]
    public async Task CriarPapel_admin_limitado_nao_semeia_permissao_que_nao_possui()
    {
        var cenario = Cenario.ComAdminLimitado();
        var handler = cenario.CriarPapelHandler();

        var acao = async () => await handler.Handle(
            new CriarPapelCommand("Folha", [DomainPermissoes.RecursosHumanosGerenciar]),
            CancellationToken.None);

        await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        cenario.PapeisAdicionados.Should().BeEmpty();
        cenario.Salvou.Should().BeFalse();
    }

    [Fact]
    public async Task CriarPapel_admin_pleno_semeia_permissao_que_possui()
    {
        var cenario = Cenario.ComAdminPleno();
        var handler = cenario.CriarPapelHandler();

        var id = await handler.Handle(
            new CriarPapelCommand("Folha", [DomainPermissoes.RecursosHumanosGerenciar]),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        cenario.PapeisAdicionados.Should().ContainSingle()
            .Which.Permissoes.Should().Contain(DomainPermissoes.RecursosHumanosGerenciar);
    }

    [Fact]
    public async Task CriarPapel_sem_permissoes_iniciais_nao_exige_I4()
    {
        var cenario = Cenario.ComAdminLimitado();
        var handler = cenario.CriarPapelHandler();

        var id = await handler.Handle(new CriarPapelCommand("Vazio"), CancellationToken.None);

        id.Should().NotBeEmpty();
        cenario.PapeisAdicionados.Should().ContainSingle().Which.Permissoes.Should().BeEmpty();
    }

    // === AA-2: PUT /usuarios/{id}/papeis (DefinirPapeisDoUsuario) ===

    [Fact]
    public async Task DefinirPapeis_admin_limitado_nao_atribui_papel_com_permissao_que_nao_possui()
    {
        var cenario = Cenario.ComAdminLimitado();
        // Papel "poderoso" preexistente (criado por alguem com mais poder, ou semeado).
        var papelPoderoso = cenario.AdicionarPapel("Admin", DomainPermissoes.FinancasGerenciar);
        var alvo = cenario.AdicionarUsuario("alvo@x.gov.br");
        var handler = cenario.DefinirPapeisHandler();

        var acao = async () => await handler.Handle(
            new DefinirPapeisDoUsuarioCommand(alvo.Id.Value, [papelPoderoso.Id.Value]),
            CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        excecao.Which.Motivo.Should().Be(MotivoConcessaoNegada.PermissaoNaoPossuida);
        alvo.Papeis.Should().BeEmpty();
        cenario.Salvou.Should().BeFalse();
    }

    [Fact]
    public async Task DefinirPapeis_admin_limitado_nao_se_auto_atribui_papel_poderoso()
    {
        // A cadeia AA-1+AA-2: o proprio concedente tenta se promover.
        var cenario = Cenario.ComAdminLimitado();
        var papelPoderoso = cenario.AdicionarPapel("Admin", DomainPermissoes.FinancasGerenciar);
        var handler = cenario.DefinirPapeisHandler();

        var acao = async () => await handler.Handle(
            new DefinirPapeisDoUsuarioCommand(cenario.Concedente.Id.Value, [papelPoderoso.Id.Value]),
            CancellationToken.None);

        await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        cenario.Salvou.Should().BeFalse();
    }

    [Fact]
    public async Task DefinirPapeis_admin_pleno_atribui_papel_que_cobre()
    {
        var cenario = Cenario.ComAdminPleno();
        var papel = cenario.AdicionarPapel("Tesouraria", DomainPermissoes.FinancasGerenciar);
        var alvo = cenario.AdicionarUsuario("alvo@x.gov.br");
        var handler = cenario.DefinirPapeisHandler();

        await handler.Handle(
            new DefinirPapeisDoUsuarioCommand(alvo.Id.Value, [papel.Id.Value]),
            CancellationToken.None);

        alvo.Papeis.Should().ContainSingle().Which.Should().Be(papel.Id);
        cenario.Salvou.Should().BeTrue();
    }

    [Fact]
    public async Task DefinirPapeis_com_lista_vazia_limpa_papeis_sem_exigir_I4()
    {
        var cenario = Cenario.ComAdminLimitado();
        var alvo = cenario.AdicionarUsuario("alvo@x.gov.br");
        var handler = cenario.DefinirPapeisHandler();

        await handler.Handle(new DefinirPapeisDoUsuarioCommand(alvo.Id.Value, []), CancellationToken.None);

        alvo.Papeis.Should().BeEmpty();
        cenario.Salvou.Should().BeTrue();
    }

    // === Cenario in-memory compartilhado ===
    private sealed class Cenario
    {
        private readonly PapelRepositorioFake _papeis = new();
        private readonly UsuarioRepositorioFake _usuarios = new();
        private readonly UnidadeRepositorioFake _unidades = new();
        private readonly UnitOfWorkFake _uow = new();
        private readonly UnidadeOrganizacional _raiz;

        private Cenario(IReadOnlyCollection<string> permissoesDoConcedente)
        {
            // Arvore minima: raiz + uma filha (cobre subarvore na expansao global).
            _raiz = UnidadeOrganizacional.CriarRaiz(Tenant, "PMX", "Prefeitura", TipoUnidade.Gabinete);
            var filha = UnidadeOrganizacional.CriarFilha(Tenant, "FAZ", "Fazenda", TipoUnidade.Secretaria, _raiz.Id);
            _unidades.Seed(_raiz, filha);

            // Papel do concedente: empacota suas permissoes; atribuido GLOBALMENTE (raiz + subarvore).
            var papelConcedente = Papel.Criar(Tenant, "Concedente", permissoesDoConcedente);
            _papeis.Seed(papelConcedente);

            Concedente = Usuario.Criar(Tenant, "Concedente", Email.De("concedente@x.gov.br"), Hash);
            Concedente.AtribuirPapel(
                papelConcedente.Id, _raiz.Id, incluiSubunidades: true,
                Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta());
            _usuarios.Seed(Concedente);
        }

        public Usuario Concedente { get; }

        public bool Salvou => _uow.Chamadas > 0;

        public IReadOnlyList<Papel> PapeisAdicionados => _papeis.Adicionados;

        public static Cenario ComAdminLimitado()
            => new([DomainPermissoes.IdentidadeUsuariosGerenciar]);

        public static Cenario ComAdminPleno()
            => new(DomainPermissoes.Todas);

        public Papel AdicionarPapel(string nome, params string[] permissoes)
        {
            var papel = Papel.Criar(Tenant, nome, permissoes);
            _papeis.Seed(papel);
            return papel;
        }

        public Usuario AdicionarUsuario(string email)
        {
            var usuario = Usuario.Criar(Tenant, "Alvo", Email.De(email), Hash);
            _usuarios.Seed(usuario);
            return usuario;
        }

        public DefinirPermissoesDoPapelHandler DefinirPermissoesHandler()
            => new(_papeis, _usuarios, _unidades, new CurrentUserConcedente(Concedente), _uow, TimeProvider.System);

        public CriarPapelHandler CriarPapelHandler()
            => new(_papeis, _usuarios, _unidades, new CurrentUserConcedente(Concedente), _uow, new TenantFake(), TimeProvider.System);

        public DefinirPapeisDoUsuarioHandler DefinirPapeisHandler()
            => new(_usuarios, _papeis, _unidades, new CurrentUserConcedente(Concedente), _uow, TimeProvider.System);
    }

    private sealed class PapelRepositorioFake : IPapelRepository
    {
        private readonly Dictionary<PapelId, Papel> _porId = [];

        public List<Papel> Adicionados { get; } = [];

        public void Seed(Papel papel) => _porId[papel.Id] = papel;

        public void Adicionar(Papel papel)
        {
            Adicionados.Add(papel);
            _porId[papel.Id] = papel;
        }

        public Task<Papel?> ObterPorIdAsync(PapelId id, CancellationToken cancellationToken)
            => Task.FromResult(_porId.GetValueOrDefault(id));

        public Task<IReadOnlyList<Papel>> ObterPorIdsAsync(IReadOnlyCollection<PapelId> ids, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Papel>>(ids.Where(_porId.ContainsKey).Select(id => _porId[id]).ToList());

        public Task<bool> NomeEmUsoAsync(string nome, CancellationToken cancellationToken)
            => Task.FromResult(_porId.Values.Any(p => string.Equals(p.Nome, nome, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Papel>> ListarAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Papel>>(_porId.Values.ToList());
    }

    private sealed class UsuarioRepositorioFake : IUsuarioRepository
    {
        private readonly Dictionary<UsuarioId, Usuario> _porId = [];

        public void Seed(Usuario usuario) => _porId[usuario.Id] = usuario;

        public void Adicionar(Usuario usuario) => _porId[usuario.Id] = usuario;

        public Task<Usuario?> ObterPorIdAsync(UsuarioId id, CancellationToken cancellationToken)
            => Task.FromResult(_porId.GetValueOrDefault(id));

        public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(null);

        public Task<bool> EmailEmUsoAsync(Email email, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Usuario>>(_porId.Values.ToList());

        public Task<Usuario?> ObterParaAutenticacaoAsync(Guid tenantId, Email email, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(null);
    }

    private sealed class UnidadeRepositorioFake : IUnidadeRepository
    {
        private readonly List<UnidadeOrganizacional> _unidades = [];

        public void Seed(params UnidadeOrganizacional[] unidades) => _unidades.AddRange(unidades);

        public void Adicionar(UnidadeOrganizacional unidade) => _unidades.Add(unidade);

        public Task<UnidadeOrganizacional?> ObterPorIdAsync(UnidadeOrganizacionalId id, CancellationToken cancellationToken)
            => Task.FromResult(_unidades.FirstOrDefault(u => u.Id == id));

        public Task<bool> CodigoEmUsoAsync(string codigo, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<IReadOnlyList<UnidadeOrganizacional>> ListarAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<UnidadeOrganizacional>>(_unidades.ToList());
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int Chamadas { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Chamadas++;
            return Task.FromResult(0);
        }
    }

    private sealed class CurrentUserConcedente(Usuario concedente) : ICurrentUser
    {
        public string? UserId => concedente.Id.Value.ToString();

        public string? UserName => "Concedente";

        public string? IpAddress => "127.0.0.1";
    }

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Tenant;

        public bool HasTenant => true;
    }
}
