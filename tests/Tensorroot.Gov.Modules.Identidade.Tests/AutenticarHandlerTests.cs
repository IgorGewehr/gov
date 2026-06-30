using FluentAssertions;
using Microsoft.Extensions.Options;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Autenticacao;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;
using Xunit;
using DomainPermissoes = Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Cobertura do caso de uso de autenticacao usando o hasher e o emissor de token REAIS sobre
/// repositorios em memoria: sucesso (token com permissoes efetivas), senha incorreta, usuario
/// inativo, usuario inexistente e e-mail mal formado — todos com a mesma excecao generica
/// (sem vazar a causa). E SEGURANCA CRITICA.
/// </summary>
public sealed class AutenticarHandlerTests
{
    private const string Segredo = "segredo-de-teste-super-secreto-com-mais-de-32-bytes-1234567890";

    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly SenhaHasher _hasher = new(workFactor: 4);

    private AutenticarHandler CriarHandler(UsuarioRepositoryFake usuarios, PapelRepositoryFake papeis)
    {
        var emissor = new EmissorToken(
            Options.Create(new JwtOptions { Secret = Segredo, DuracaoMinutos = 60 }),
            TimeProvider.System);

        return new AutenticarHandler(usuarios, papeis, new UnidadeRepositoryFake(), _hasher, emissor, new UnitOfWorkFake(), TimeProvider.System);
    }

    private Usuario CriarUsuario(string email, string senha, bool ativo = true, IEnumerable<PapelId>? papeis = null)
    {
        var usuario = Usuario.Criar(TenantA, "Maria", Email.De(email), _hasher.Hash(senha), papeis);
        if (!ativo)
        {
            usuario.Desativar();
        }

        return usuario;
    }

    [Fact]
    public async Task Autenticar_com_credenciais_validas_retorna_token_e_permissoes_efetivas()
    {
        var papel = Papel.Criar(TenantA, "Fiscal", [DomainPermissoes.TributosVer, DomainPermissoes.TributosGerenciar]);
        var usuario = CriarUsuario("maria@x.gov.br", "Senha@Forte123", papeis: [papel.Id]);

        var usuarios = new UsuarioRepositoryFake(usuario);
        var papeis = new PapelRepositoryFake(papel);
        var handler = CriarHandler(usuarios, papeis);

        var resultado = await handler.Handle(
            new AutenticarCommand(TenantA, "maria@x.gov.br", "Senha@Forte123", "Prefeitura X"),
            CancellationToken.None);

        resultado.UsuarioId.Should().Be(usuario.Id.Value);
        resultado.TenantId.Should().Be(TenantA);
        resultado.Email.Should().Be("maria@x.gov.br");
        resultado.PermissoesEfetivas.Should().BeEquivalentTo(
            new[] { DomainPermissoes.TributosVer, DomainPermissoes.TributosGerenciar });
        resultado.Token.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Autenticar_com_senha_incorreta_falha_de_forma_generica()
    {
        var usuario = CriarUsuario("maria@x.gov.br", "Senha@Forte123");
        var handler = CriarHandler(new UsuarioRepositoryFake(usuario), new PapelRepositoryFake());

        var acao = () => handler.Handle(
            new AutenticarCommand(TenantA, "maria@x.gov.br", "SenhaErrada"),
            CancellationToken.None);

        await acao.Should().ThrowAsync<AutenticacaoFalhouException>();
    }

    [Fact]
    public async Task Autenticar_usuario_inativo_e_negado_mesmo_com_senha_correta()
    {
        var usuario = CriarUsuario("maria@x.gov.br", "Senha@Forte123", ativo: false);
        var handler = CriarHandler(new UsuarioRepositoryFake(usuario), new PapelRepositoryFake());

        var acao = () => handler.Handle(
            new AutenticarCommand(TenantA, "maria@x.gov.br", "Senha@Forte123"),
            CancellationToken.None);

        await acao.Should().ThrowAsync<AutenticacaoFalhouException>();
    }

    [Fact]
    public async Task Autenticar_usuario_inexistente_falha_de_forma_generica()
    {
        var handler = CriarHandler(new UsuarioRepositoryFake(), new PapelRepositoryFake());

        var acao = () => handler.Handle(
            new AutenticarCommand(TenantA, "ninguem@x.gov.br", "Senha@Forte123"),
            CancellationToken.None);

        await acao.Should().ThrowAsync<AutenticacaoFalhouException>();
    }

    [Fact]
    public async Task Autenticar_com_email_mal_formado_falha_de_forma_generica()
    {
        var handler = CriarHandler(new UsuarioRepositoryFake(), new PapelRepositoryFake());

        var acao = () => handler.Handle(
            new AutenticarCommand(TenantA, "nao-e-email", "Senha@Forte123"),
            CancellationToken.None);

        await acao.Should().ThrowAsync<AutenticacaoFalhouException>();
    }

    [Fact]
    public async Task Apos_exceder_o_limite_de_tentativas_a_conta_e_bloqueada_mesmo_com_senha_correta()
    {
        // P7: 5 falhas consecutivas (limite do handler) -> bloqueio; depois nem a senha correta passa.
        var usuario = CriarUsuario("maria@x.gov.br", "Senha@Forte123");
        var handler = CriarHandler(new UsuarioRepositoryFake(usuario), new PapelRepositoryFake());

        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var erro = () => handler.Handle(
                new AutenticarCommand(TenantA, "maria@x.gov.br", "SenhaErrada"), CancellationToken.None);
            await erro.Should().ThrowAsync<AutenticacaoFalhouException>();
        }

        usuario.LockoutEnd.Should().NotBeNull("apos 5 falhas consecutivas a conta deve bloquear");

        // Mesmo com a senha CORRETA, a conta bloqueada e negada (com a mesma excecao generica).
        var aindaBloqueado = () => handler.Handle(
            new AutenticarCommand(TenantA, "maria@x.gov.br", "Senha@Forte123"), CancellationToken.None);
        await aindaBloqueado.Should().ThrowAsync<AutenticacaoFalhouException>();
    }

    [Fact]
    public async Task Autenticar_de_outro_tenant_nao_encontra_o_usuario()
    {
        var outroTenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var usuario = CriarUsuario("maria@x.gov.br", "Senha@Forte123");
        var handler = CriarHandler(new UsuarioRepositoryFake(usuario), new PapelRepositoryFake());

        var acao = () => handler.Handle(
            new AutenticarCommand(outroTenant, "maria@x.gov.br", "Senha@Forte123"),
            CancellationToken.None);

        await acao.Should().ThrowAsync<AutenticacaoFalhouException>();
    }
}

/// <summary>Unidade de trabalho em memoria (no-op): a mutacao do agregado ja ocorre no objeto fake.</summary>
internal sealed class UnitOfWorkFake : IUnitOfWork
{
    public int Confirmacoes { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Confirmacoes++;
        return Task.FromResult(0);
    }
}

/// <summary>Repositorio de usuarios em memoria que filtra por tenant explicito na autenticacao.</summary>
internal sealed class UsuarioRepositoryFake(params Usuario[] usuarios) : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = [.. usuarios];

    public void Adicionar(Usuario usuario) => _usuarios.Add(usuario);

    public Task<Usuario?> ObterPorIdAsync(UsuarioId id, CancellationToken cancellationToken)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.Email == email));

    public Task<bool> EmailEmUsoAsync(Email email, CancellationToken cancellationToken)
        => Task.FromResult(_usuarios.Any(u => u.Email == email));

    public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Usuario>>(_usuarios);

    public Task<Usuario?> ObterParaAutenticacaoAsync(Guid tenantId, Email email, CancellationToken cancellationToken)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.TenantId == tenantId && u.Email == email));
}

/// <summary>Repositorio de papeis em memoria.</summary>
internal sealed class PapelRepositoryFake(params Papel[] papeis) : IPapelRepository
{
    private readonly List<Papel> _papeis = [.. papeis];

    public void Adicionar(Papel papel) => _papeis.Add(papel);

    public Task<Papel?> ObterPorIdAsync(PapelId id, CancellationToken cancellationToken)
        => Task.FromResult(_papeis.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Papel>> ObterPorIdsAsync(IReadOnlyCollection<PapelId> ids, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Papel>>(_papeis.Where(p => ids.Contains(p.Id)).ToList());

    public Task<bool> NomeEmUsoAsync(string nome, CancellationToken cancellationToken)
        => Task.FromResult(_papeis.Any(p => p.Nome == nome));

    public Task<IReadOnlyList<Papel>> ListarAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Papel>>(_papeis);
}

/// <summary>Repositorio de Unidades Organizacionais em memoria (arvore de UOs do tenant).</summary>
internal sealed class UnidadeRepositoryFake(params UnidadeOrganizacional[] unidades) : IUnidadeRepository
{
    private readonly List<UnidadeOrganizacional> _unidades = [.. unidades];

    public void Adicionar(UnidadeOrganizacional unidade) => _unidades.Add(unidade);

    public Task<UnidadeOrganizacional?> ObterPorIdAsync(UnidadeOrganizacionalId id, CancellationToken cancellationToken)
        => Task.FromResult(_unidades.FirstOrDefault(u => u.Id == id));

    public Task<bool> CodigoEmUsoAsync(string codigo, CancellationToken cancellationToken)
        => Task.FromResult(_unidades.Any(u => string.Equals(u.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<UnidadeOrganizacional>> ListarAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<UnidadeOrganizacional>>(_unidades);
}
