using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Base de testes de integracao do modulo Administracao sobre SQLite em memoria:
/// abre uma unica conexao por classe de teste (o schema vive enquanto a conexao estiver aberta)
/// e cria contextos com os interceptors de tenant e de auditoria, a imagem de PatrimonioTestBase.
/// </summary>
public abstract class AdministracaoTestBase : IDisposable
{
    /// <summary>Tenant A — usado como tenant principal dos cenarios.</summary>
    protected static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Tenant B — usado para provar o isolamento (Global Query Filter).</summary>
    protected static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>Calendario de dias uteis deterministico para os testes (fins de semana; sem feriados).</summary>
    protected static readonly ICalendarioDiasUteis Calendario = new CalendarioTesteSemFeriados();

    /// <summary>Parametro padrao do prazo de divulgacao no PNCP (20 d.u. — art. 94) para os testes.</summary>
    protected static readonly PrazoPncpParametro PrazoDivulgacaoPadrao =
        new(20, UnidadePrazo.DiasUteis, "Lei 14.133/2021 art. 94");

    private readonly SqliteConnection _connection;

    /// <summary>Abre a conexao SQLite em memoria compartilhada pelos contextos do teste.</summary>
    protected AdministracaoTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>Cria um <see cref="AdministracaoDbContext"/> no contexto do tenant informado, com auditoria e filtro por tenant.</summary>
    /// <param name="tenantId">Tenant cujo escopo o contexto enxerga.</param>
    /// <returns>Contexto pronto (schema criado).</returns>
    protected AdministracaoDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<AdministracaoDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new AdministracaoDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

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

/// <summary>
/// Calendario deterministico para teste: apenas fins de semana sao nao-uteis (sem feriados). Suficiente
/// para exercitar a resolucao de prazo PNCP sem depender da fonte de feriados por tenant.
/// </summary>
internal sealed class CalendarioTesteSemFeriados : ICalendarioDiasUteis
{
    public DateOnly AdicionarDiasUteis(DateOnly inicio, int diasUteis)
    {
        var data = inicio;
        var restantes = diasUteis;
        while (restantes > 0)
        {
            data = data.AddDays(1);
            if (EhDiaUtil(data))
            {
                restantes--;
            }
        }

        return data;
    }

    public bool EhDiaUtil(DateOnly data)
        => data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    public DateOnly ProximoDiaUtil(DateOnly data)
    {
        var atual = data;
        while (!EhDiaUtil(atual))
        {
            atual = atual.AddDays(1);
        }

        return atual;
    }

    public int DiasUteisEntre(DateOnly a, DateOnly b)
    {
        if (b < a)
        {
            return -DiasUteisEntre(b, a);
        }

        var contagem = 0;
        var atual = a;
        while (atual < b)
        {
            atual = atual.AddDays(1);
            if (EhDiaUtil(atual))
            {
                contagem++;
            }
        }

        return contagem;
    }
}
