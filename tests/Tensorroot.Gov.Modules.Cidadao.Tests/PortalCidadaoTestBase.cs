using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Cidadao.Tests;

/// <summary>
/// Base de testes de integracao do Portal do Cidadao sobre SQLite em memoria. Mantem UMA conexao por
/// classe (o schema vive enquanto a conexao estiver aberta) e cria os tres DbContexts envolvidos
/// (Cidadao/Tributos/Protocolo) escopados a um tenant, com os interceptors de tenant e auditoria — de
/// modo a exercitar a CADEIA REAL (conta-cidadao + leitura cidada de Tributos/Protocolo) ponta a ponta.
/// </summary>
public abstract class PortalCidadaoTestBase : IDisposable
{
    /// <summary>Tenant A — principal dos cenarios.</summary>
    protected static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Tenant B — prova do isolamento (Global Query Filter).</summary>
    protected static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // Uma conexao DEDICADA por modulo (cada modulo tem seu schema/banco no produto). O schema vive
    // enquanto a conexao estiver aberta — mantida por toda a classe de teste.
    private readonly SqliteConnection _cidadaoConn = AbrirConexao();
    private readonly SqliteConnection _tributosConn = AbrirConexao();
    private readonly SqliteConnection _protocoloConn = AbrirConexao();

    private static SqliteConnection AbrirConexao()
    {
        var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        return conexao;
    }

    /// <summary>Cria o <see cref="CidadaoDbContext"/> no escopo do tenant informado.</summary>
    protected CidadaoDbContext CriarCidadao(Guid tenantId)
    {
        var tenantContext = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<CidadaoDbContext>()
            .UseSqlite(_cidadaoConn)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;
        var contexto = new CidadaoDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    /// <summary>Cria o <see cref="TributosDbContext"/> no escopo do tenant informado.</summary>
    protected TributosDbContext CriarTributos(Guid tenantId)
    {
        var tenantContext = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(_tributosConn)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;
        var contexto = new TributosDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    /// <summary>Cria o <see cref="ProtocoloDbContext"/> no escopo do tenant informado.</summary>
    protected ProtocoloDbContext CriarProtocolo(Guid tenantId)
    {
        var tenantContext = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<ProtocoloDbContext>()
            .UseSqlite(_protocoloConn)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;
        var contexto = new ProtocoloDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cidadaoConn.Dispose();
        _tributosConn.Dispose();
        _protocoloConn.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

/// <summary>
/// Fake de <see cref="Tensorroot.Gov.Modules.Cidadao.Application.Abstractions.IConsultaCidadaoEmEscopoDedicado"/>
/// para teste: ao inves de abrir um escopo de DI real, mantem as portas concretas (Tributos/Protocolo) ja
/// construidas e as injeta na funcao por TIPO. Preserva o contrato (a consulta recebe a porta resolvida),
/// sem precisar de IServiceScopeFactory. No produto, o ApiHost abre o escopo dedicado de verdade (guarda H5).
/// </summary>
internal sealed class ConsultaCidadaoEmEscopoDedicadoFake(params object[] portas)
    : Tensorroot.Gov.Modules.Cidadao.Application.Abstractions.IConsultaCidadaoEmEscopoDedicado
{
    public Task<TResultado> ExecutarAsync<TConsulta, TResultado>(
        Func<TConsulta, CancellationToken, Task<TResultado>> consulta,
        CancellationToken cancellationToken)
        where TConsulta : notnull
    {
        ArgumentNullException.ThrowIfNull(consulta);
        var porta = portas.OfType<TConsulta>().FirstOrDefault()
            ?? throw new InvalidOperationException($"Nenhuma porta do tipo {typeof(TConsulta).Name} registrada no fake.");
        return consulta(porta, cancellationToken);
    }
}

internal sealed class CurrentUserFake : ICurrentUser
{
    public string? UserId => "teste";

    public string? UserName => "Usuario de Teste";

    public string? IpAddress => "127.0.0.1";
}

/// <summary>ICurrentUser de teste com subject ("sub") configuravel — simula o principal cidadao.</summary>
internal sealed class CurrentUserCidadaoFake(Guid? sub) : ICurrentUser
{
    public string? UserId => sub?.ToString();

    public string? UserName => "Cidadao de Teste";

    public string? IpAddress => "127.0.0.1";
}

/// <summary>IRegistroAcessoSensivel de teste que conta negativas e leituras seladas (trilha LGPD).</summary>
internal sealed class RegistroAcessoSpy : Tensorroot.Gov.BuildingBlocks.Application.Abstractions.IRegistroAcessoSensivel
{
    public int Leituras { get; private set; }

    public int Negativas { get; private set; }

    public Task RegistrarAsync(string entidade, string? entidadeId, Tensorroot.Gov.SharedKernel.BaseLegalLgpd baseLegal, CancellationToken cancellationToken = default)
    {
        Leituras++;
        return Task.CompletedTask;
    }

    public Task RegistrarNegacaoAsync(string entidade, string? entidadeId, Tensorroot.Gov.SharedKernel.BaseLegalLgpd baseLegalRejeitada, CancellationToken cancellationToken = default)
    {
        Negativas++;
        return Task.CompletedTask;
    }
}
