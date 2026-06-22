using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Tests;

/// <summary>
/// Base de testes de integracao do modulo AssistenciaSocial sobre SQLite em memoria: abre uma
/// unica conexao por classe de teste (o schema vive enquanto a conexao estiver aberta) e cria
/// contextos com os interceptors de tenant e de auditoria, a imagem de PatrimonioTestBase.
/// </summary>
public abstract class AssistenciaSocialTestBase : IDisposable
{
    /// <summary>Tenant A — usado como tenant principal dos cenarios.</summary>
    protected static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Tenant B — usado para provar o isolamento (Global Query Filter).</summary>
    protected static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // NIS validos (DV modulo 11) e CPFs validos, usados nos cenarios.
    /// <summary>NIS valido do responsavel familiar (cenario principal).</summary>
    protected const string NisValido = "12345678919";

    /// <summary>Segundo NIS valido (familias adicionais).</summary>
    protected const string NisValido2 = "23456789129";

    /// <summary>CPF valido do responsavel familiar.</summary>
    protected const string CpfResponsavel = "12345678909";

    /// <summary>Segundo CPF valido (membro adicional).</summary>
    protected const string CpfMembro = "98765432100";

    private readonly SqliteConnection _connection;

    /// <summary>Abre a conexao SQLite em memoria compartilhada pelos contextos do teste.</summary>
    protected AssistenciaSocialTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>Cria um <see cref="AssistenciaSocialDbContext"/> no escopo do tenant informado, com auditoria e filtro por tenant.</summary>
    /// <param name="tenantId">Tenant cujo escopo o contexto enxerga.</param>
    /// <returns>Contexto pronto (schema criado).</returns>
    protected AssistenciaSocialDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<AssistenciaSocialDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new AssistenciaSocialDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    /// <summary>Cria um endereco territorializado de teste no territorio informado.</summary>
    /// <param name="territorio">Territorio de cobertura do CRAS.</param>
    /// <returns>Endereco territorializado.</returns>
    protected static EnderecoTerritorializado Endereco(string territorio = "Centro")
        => EnderecoTerritorializado.Create("Rua das Flores, 100", "Maximiliano de Almeida", "99975000", territorio);

    /// <summary>Cria um valor monetario de teste.</summary>
    /// <param name="valor">Montante.</param>
    /// <returns>Valor monetario.</returns>
    protected static ValorMonetario Dinheiro(decimal valor) => ValorMonetario.De(valor);

    /// <summary>Cria um NIS de dominio a partir de uma string valida.</summary>
    /// <param name="nis">NIS valido.</param>
    /// <returns>VO <see cref="Nis"/>.</returns>
    protected static Nis NovoNis(string nis = NisValido) => Nis.Create(nis);

    /// <summary>Cria um CPF de dominio a partir de uma string valida.</summary>
    /// <param name="cpf">CPF valido.</param>
    /// <returns>VO <see cref="Cpf"/>.</returns>
    protected static Cpf NovoCpf(string cpf = CpfResponsavel) => Cpf.Create(cpf);

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

internal sealed class CurrentUserFake : ICurrentUser
{
    public string? UserId => "teste";

    public string? UserName => "Usuario de Teste";

    public string? IpAddress => "127.0.0.1";
}
