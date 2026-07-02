using FluentAssertions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// SEGURANCA CRITICA — fecha os achados W10.6 ID-1 (P0) e ID-2 da auditoria de PLATAFORMA: os comandos
/// administrativos sobre a CONTA de um usuario (reset de senha, edicao de e-mail, ativar/desativar) NAO
/// recebiam a verificacao de ESCOPO/I4 que a atribuicao/revogacao de papeis ja impoe. Um admin escopado
/// a uma sub-UO podia resetar a senha do ADMIN-RAIZ do tenant e tomar a conta (escalonamento de
/// privilegio completo). Estes testes PROVAM que o handler agora NEGA (403 na borda) e NAO grava:
/// <list type="bullet">
/// <item>anti-escalacao (ID-1): admin de sub-UO nao age sobre quem detem permissao/papel que ele nao tem;</item>
/// <item>contencao de escopo (ID-2): admin de uma subarvore nao alcanca conta ancorada em outra;</item>
/// <item>caminho legitimo: admin pleno (Permissoes.Todas na raiz) AGE sobre um usuario escopado.</item>
/// </list>
/// </summary>
public sealed class AutorizacaoAdminUsuarioTests
{
    private const string Hash = "$2a$04$hashqualquerparaoteste0000000000000000000000000000";
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // === ID-1 (P0): reset de senha — anti-tomada de conta do admin-raiz ===

    [Fact]
    public async Task AlterarSenha_admin_escopado_NAO_reseta_senha_do_admin_raiz()
    {
        var cenario = Cenario.Novo();
        // Alvo = admin-raiz: papel com Permissoes.Todas, atribuido na RAIZ com subarvore (acesso global).
        var raizAdmin = cenario.AdicionarUsuarioComPapelGlobal("raiz@x.gov.br", "AdminRaiz", DomainPermissoes.Todas);
        // Admin atual = escopado: so identidade.usuarios.gerenciar, ancorado na FILHA Fazenda.
        var handler = cenario.AlterarSenhaHandlerComoAdminEscopado();

        var senhaAntes = raizAdmin.SenhaHash;
        var acao = async () => await handler.Handle(
            new AlterarSenhaCommand(raizAdmin.Id.Value, "NovaSenhaForte123"), CancellationToken.None);

        // Negado: o admin-raiz esta ancorado na RAIZ (fora da subarvore Fazenda do admin) E detem o
        // conjunto global — qualquer uma das duas guardas (contencao de escopo OU anti-escalacao) basta
        // para NEGAR. O invariante de seguranca e o mesmo: a senha NAO muda e nada e persistido.
        var excecao = await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        excecao.Which.Motivo.Should().BeOneOf(
            MotivoConcessaoNegada.ForaDoEscopoAdministrativo, MotivoConcessaoNegada.PermissaoNaoPossuida);
        raizAdmin.SenhaHash.Should().Be(senhaAntes, "a senha do admin-raiz NAO pode ter sido trocada");
        cenario.Salvou.Should().BeFalse("nenhuma mutacao pode ser persistida quando a autorizacao nega");
    }

    [Fact]
    public async Task AlterarSenha_admin_escopado_NAO_reseta_quem_detem_permissao_que_ele_nao_tem_no_MESMO_escopo()
    {
        // Prova DEDICADA da anti-escalacao (gate c / I4): o admin COBRE a UO do alvo (mesma subarvore
        // Fazenda), mas o alvo detem uma permissao (financas.gerenciar) que o admin NAO possui. Negar
        // por PermissaoNaoPossuida — "nao age sobre quem e mais poderoso que voce", mesmo no seu escopo.
        var cenario = Cenario.Novo();
        var alvo = cenario.AdicionarUsuarioComPapelNaUnidade(
            "poderoso@x.gov.br", "Tesoureiro", cenario.UnidadeFazenda.Id, DomainPermissoes.FinancasGerenciar);
        var handler = cenario.AlterarSenhaHandlerComoAdminEscopado();

        var senhaAntes = alvo.SenhaHash;
        var acao = async () => await handler.Handle(
            new AlterarSenhaCommand(alvo.Id.Value, "NovaSenhaForte123"), CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        excecao.Which.Motivo.Should().Be(MotivoConcessaoNegada.PermissaoNaoPossuida);
        excecao.Which.PermissaoFaltante.Should().Be(DomainPermissoes.FinancasGerenciar);
        alvo.SenhaHash.Should().Be(senhaAntes);
        cenario.Salvou.Should().BeFalse();
    }

    [Fact]
    public async Task AlterarSenha_admin_pleno_RESETA_senha_de_usuario_escopado()
    {
        var cenario = Cenario.Novo();
        // Alvo escopado (apenas leitura de saude na sub-UO Saude): dominado pelo admin pleno.
        var alvo = cenario.AdicionarUsuarioComPapelNaUnidade(
            "alvo@x.gov.br", "Operador", cenario.UnidadeSaude.Id, DomainPermissoes.SaudeVer);
        var handler = cenario.AlterarSenhaHandlerComoAdminPleno();

        await handler.Handle(new AlterarSenhaCommand(alvo.Id.Value, "NovaSenhaForte123"), CancellationToken.None);

        cenario.Salvou.Should().BeTrue("o admin pleno cobre o escopo e domina o alvo — caminho legitimo");
    }

    [Fact]
    public async Task AlterarSenha_admin_de_outra_subarvore_NAO_alcanca_usuario_fora_do_escopo()
    {
        var cenario = Cenario.Novo();
        // Alvo ancorado na subarvore SAUDE; admin escopado administra apenas a subarvore FAZENDA.
        var alvo = cenario.AdicionarUsuarioComPapelNaUnidade(
            "saude@x.gov.br", "AgenteSaude", cenario.UnidadeSaude.Id, DomainPermissoes.IdentidadeUsuariosGerenciar);
        var handler = cenario.AlterarSenhaHandlerComoAdminEscopado();

        var acao = async () => await handler.Handle(
            new AlterarSenhaCommand(alvo.Id.Value, "NovaSenhaForte123"), CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        excecao.Which.Motivo.Should().Be(MotivoConcessaoNegada.ForaDoEscopoAdministrativo);
        cenario.Salvou.Should().BeFalse();
    }

    // === ID-2: editar/desativar — mesma classe ===

    [Fact]
    public async Task EditarUsuario_admin_escopado_NAO_edita_o_admin_raiz()
    {
        var cenario = Cenario.Novo();
        var raizAdmin = cenario.AdicionarUsuarioComPapelGlobal("raiz@x.gov.br", "AdminRaiz", DomainPermissoes.Todas);
        var handler = cenario.EditarUsuarioHandlerComoAdminEscopado();

        var acao = async () => await handler.Handle(
            new EditarUsuarioCommand(raizAdmin.Id.Value, "Novo Nome", "novo@x.gov.br"), CancellationToken.None);

        await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        raizAdmin.Email.Valor.Should().Be("raiz@x.gov.br", "o e-mail de login do admin-raiz NAO pode mudar");
        cenario.Salvou.Should().BeFalse();
        cenario.LoginCentralAtualizado.Should().BeFalse("o indice central nao pode ser tocado quando nega");
    }

    [Fact]
    public async Task DesativarUsuario_admin_escopado_NAO_desativa_o_admin_raiz()
    {
        var cenario = Cenario.Novo();
        var raizAdmin = cenario.AdicionarUsuarioComPapelGlobal("raiz@x.gov.br", "AdminRaiz", DomainPermissoes.Todas);
        var handler = cenario.DesativarUsuarioHandlerComoAdminEscopado();

        var acao = async () => await handler.Handle(
            new DesativarUsuarioCommand(raizAdmin.Id.Value), CancellationToken.None);

        await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        raizAdmin.Ativo.Should().BeTrue("nao se pode negar servico ao admin-raiz fora do escopo");
        cenario.Salvou.Should().BeFalse();
    }

    // === S1 (achado AA-2): criar usuario COM papeis iniciais tambem aplica a prova I4 ===

    [Fact]
    public async Task CriarUsuario_admin_escopado_NAO_concede_papel_fora_do_seu_escopo()
    {
        // Admin escopado (so identidade.usuarios.gerenciar na Fazenda) tenta CRIAR usuario ja com um
        // papel de Financas — permissao que ele nao possui. Antes do S1, criar-com-papeis pulava a I4.
        var cenario = Cenario.Novo();
        var papelFinancas = cenario.SemearPapel("Tesoureiro", DomainPermissoes.FinancasGerenciar);
        var handler = cenario.CriarUsuarioHandlerComoAdminEscopado();

        var acao = async () => await handler.Handle(
            new CriarUsuarioCommand("Novo", "novo@x.gov.br", "SenhaForte123", [papelFinancas.Id.Value]),
            CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<ConcessaoNaoAutorizadaException>();
        excecao.Which.Motivo.Should().BeOneOf(
            MotivoConcessaoNegada.PermissaoNaoPossuida, MotivoConcessaoNegada.ForaDoEscopoAdministrativo);
        cenario.Salvou.Should().BeFalse("nada e persistido quando a autorizacao nega");
    }

    [Fact]
    public async Task CriarUsuario_admin_pleno_concede_papel_inicial()
    {
        // Caminho legitimo: admin pleno (Permissoes.Todas na raiz) cobre o escopo global e concede.
        var cenario = Cenario.Novo();
        var papelSaude = cenario.SemearPapel("OperadorSaude", DomainPermissoes.SaudeVer);
        var handler = cenario.CriarUsuarioHandlerComoAdminPleno();

        var id = await handler.Handle(
            new CriarUsuarioCommand("Zelia", "zelia@x.gov.br", "SenhaForte123", [papelSaude.Id.Value]),
            CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        cenario.Salvou.Should().BeTrue("o admin pleno cobre o escopo global — caminho legitimo");
    }

    // === Cenario in-memory ===
    private sealed class Cenario
    {
        private readonly PapelRepositorioFake _papeis = new();
        private readonly UsuarioRepositorioFake _usuarios = new();
        private readonly UnidadeRepositorioFake _unidades = new();
        private readonly UnitOfWorkFake _uow = new();
        private readonly SenhaHasherFake _hasher = new();
        private readonly RegistroLoginCentralFake _loginCentral = new();
        private readonly UnidadeOrganizacional _raiz;
        private readonly Usuario _adminEscopado;
        private readonly Usuario _adminPleno;

        private Cenario()
        {
            // Arvore: raiz + duas subarvores irmas (Fazenda e Saude).
            _raiz = UnidadeOrganizacional.CriarRaiz(Tenant, "PMX", "Prefeitura", TipoUnidade.Gabinete);
            UnidadeFazenda = UnidadeOrganizacional.CriarFilha(Tenant, "FAZ", "Fazenda", TipoUnidade.Secretaria, _raiz.Id);
            UnidadeSaude = UnidadeOrganizacional.CriarFilha(Tenant, "SAU", "Saude", TipoUnidade.Secretaria, _raiz.Id);
            _unidades.Seed(_raiz, UnidadeFazenda, UnidadeSaude);

            // Admin ESCOPADO: so identidade.usuarios.gerenciar, ancorado na subarvore FAZENDA.
            var papelEscopado = Papel.Criar(Tenant, "AdminFazenda", [DomainPermissoes.IdentidadeUsuariosGerenciar]);
            _papeis.Seed(papelEscopado);
            _adminEscopado = Usuario.Criar(Tenant, "Admin Fazenda", Email.De("adminfaz@x.gov.br"), Hash);
            _adminEscopado.AtribuirPapel(
                papelEscopado.Id, UnidadeFazenda.Id, incluiSubunidades: true,
                Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta());
            _usuarios.Seed(_adminEscopado);

            // Admin PLENO: Permissoes.Todas na RAIZ com subarvore (acesso global) — caminho legitimo.
            var papelPleno = Papel.Criar(Tenant, "AdminPleno", DomainPermissoes.Todas);
            _papeis.Seed(papelPleno);
            _adminPleno = Usuario.Criar(Tenant, "Admin Pleno", Email.De("pleno@x.gov.br"), Hash);
            _adminPleno.AtribuirPapel(
                papelPleno.Id, _raiz.Id, incluiSubunidades: true,
                Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta());
            _usuarios.Seed(_adminPleno);
        }

        public UnidadeOrganizacional UnidadeFazenda { get; }

        public UnidadeOrganizacional UnidadeSaude { get; }

        public bool Salvou => _uow.Chamadas > 0;

        public bool LoginCentralAtualizado => _loginCentral.Atualizado;

        public static Cenario Novo() => new();

        public Usuario AdicionarUsuarioComPapelGlobal(string email, string nomePapel, IReadOnlyCollection<string> permissoes)
            => AdicionarUsuarioComPapelNaUnidade(email, nomePapel, _raiz.Id, permissoes, incluiSubunidades: true);

        public Usuario AdicionarUsuarioComPapelNaUnidade(
            string email, string nomePapel, UnidadeOrganizacionalId unidadeId, params string[] permissoes)
            => AdicionarUsuarioComPapelNaUnidade(email, nomePapel, unidadeId, permissoes, incluiSubunidades: true);

        private Usuario AdicionarUsuarioComPapelNaUnidade(
            string email,
            string nomePapel,
            UnidadeOrganizacionalId unidadeId,
            IReadOnlyCollection<string> permissoes,
            bool incluiSubunidades)
        {
            var papel = Papel.Criar(Tenant, nomePapel, permissoes);
            _papeis.Seed(papel);
            var usuario = Usuario.Criar(Tenant, nomePapel, Email.De(email), Hash);
            usuario.AtribuirPapel(
                papel.Id, unidadeId, incluiSubunidades,
                Vigencia.Aberta(DateTimeOffset.UtcNow), OrigemAtribuicao.Direta());
            _usuarios.Seed(usuario);
            return usuario;
        }

        public AlterarSenhaHandler AlterarSenhaHandlerComoAdminEscopado()
            => new(_usuarios, _hasher, Guarda(_adminEscopado), _uow);

        public AlterarSenhaHandler AlterarSenhaHandlerComoAdminPleno()
            => new(_usuarios, _hasher, Guarda(_adminPleno), _uow);

        public EditarUsuarioHandler EditarUsuarioHandlerComoAdminEscopado()
            => new(_usuarios, _loginCentral, Guarda(_adminEscopado), _uow, new TenantFake());

        public DesativarUsuarioHandler DesativarUsuarioHandlerComoAdminEscopado()
            => new(_usuarios, Guarda(_adminEscopado), _uow);

        public Papel SemearPapel(string nome, params string[] permissoes)
        {
            var papel = Papel.Criar(Tenant, nome, permissoes);
            _papeis.Seed(papel);
            return papel;
        }

        public CriarUsuarioHandler CriarUsuarioHandlerComoAdminEscopado()
            => new(_usuarios, _papeis, _hasher, _loginCentral, _uow, _unidades, new TenantFake(), new CurrentUserDe(_adminEscopado), TimeProvider.System);

        public CriarUsuarioHandler CriarUsuarioHandlerComoAdminPleno()
            => new(_usuarios, _papeis, _hasher, _loginCentral, _uow, _unidades, new TenantFake(), new CurrentUserDe(_adminPleno), TimeProvider.System);

        private Application.Internal.AutorizacaoAdminUsuario Guarda(Usuario admin)
            => new(
                _usuarios,
                _papeis,
                _unidades,
                new CurrentUserDe(admin),
                TimeProvider.System,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Application.Internal.AutorizacaoAdminUsuario>.Instance);
    }

    private sealed class PapelRepositorioFake : IPapelRepository
    {
        private readonly Dictionary<PapelId, Papel> _porId = [];

        public void Seed(Papel papel) => _porId[papel.Id] = papel;

        public void Adicionar(Papel papel) => _porId[papel.Id] = papel;

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

    private sealed class SenhaHasherFake : ISenhaHasher
    {
        public string Hash(string senha) => "hash:" + senha;

        public bool Verificar(string senha, string hash) => hash == "hash:" + senha;
    }

    private sealed class RegistroLoginCentralFake : IRegistroLoginCentral
    {
        public bool Atualizado { get; private set; }

        public Task RegistrarAsync(string email, Guid tenantId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task AtualizarAsync(string emailAntigo, string emailNovo, Guid tenantId, CancellationToken cancellationToken)
        {
            Atualizado = true;
            return Task.CompletedTask;
        }
    }

    private sealed class CurrentUserDe(Usuario usuario) : ICurrentUser
    {
        public string? UserId => usuario.Id.Value.ToString();

        public string? UserName => "Admin";

        public string? IpAddress => "127.0.0.1";
    }

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Tenant;

        public bool HasTenant => true;
    }
}
