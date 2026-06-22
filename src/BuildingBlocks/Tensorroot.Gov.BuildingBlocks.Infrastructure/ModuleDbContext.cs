using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure;

/// <summary>
/// DbContext base de um módulo: aplica schema isolado, mapeia Outbox e Audit Trail e
/// instala o Global Query Filter por tenant. Cada módulo herda e declara seus DbSets.
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantUnidadeContext? _unidadeContext;

    /// <summary>Inicializa o contexto base do módulo.</summary>
    /// <param name="options">Opções do DbContext.</param>
    /// <param name="tenantContext">Contexto do tenant atual.</param>
    /// <param name="holder">
    /// Suporte da unidade de trabalho por escopo (injetado pelo DI em runtime). Ao ser construído,
    /// o contexto se registra como o contexto ATIVO do escopo, para que <see cref="ModuleUnitOfWork"/>
    /// confirme o DbContext correto. Nulo em construções diretas (migração/testes), onde não há UoW.
    /// </param>
    /// <param name="unidadeContext">
    /// Contexto de escopo de Unidade Organizacional (UO) da requisição. Nulo em construções diretas
    /// (migração/testes/jobs): nesse caso o filtro de UO NÃO é aplicado (igual ao override de tenant).
    /// </param>
    protected ModuleDbContext(
        DbContextOptions options,
        ITenantContext tenantContext,
        ScopeDbContextHolder? holder = null,
        ITenantUnidadeContext? unidadeContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
        _unidadeContext = unidadeContext;
        holder?.Definir(this);
    }

    /// <summary>Nome do schema isolado do módulo (ex.: "tributos").</summary>
    public abstract string Schema { get; }

    /// <summary>TenantId atual usado pelo Global Query Filter (reavaliado por consulta).</summary>
    public Guid CurrentTenantId => _tenantContext.HasTenant ? _tenantContext.TenantId : Guid.Empty;

    /// <summary>
    /// Indica se o filtro de UO deve ser aplicado nesta consulta (MODELO §5). Falso quando não há
    /// contexto de UO resolvido (jobs/sistema) — não filtra, igual ao override de tenant.
    /// </summary>
    public bool FiltrarPorUnidade => _unidadeContext?.DeveFiltrarPorUnidade ?? false;

    /// <summary>Conjunto de UOs (ids) que o sujeito pode ler, reavaliado por consulta.</summary>
    public IReadOnlyCollection<Guid> UnidadesPermitidas => _unidadeContext?.UnidadesPermitidas ?? [];

    /// <summary>Mensagens de Outbox pendentes de publicação.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Trilha de auditoria imutável do módulo.</summary>
    public DbSet<AuditTrail> AuditTrail => Set<AuditTrail>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyOutboxAndAuditMappings();
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyTenantAndUnidadeQueryFilters(this);
    }
}
