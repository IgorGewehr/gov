using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo Identidade (schema isolado "identidade"), no banco DEDICADO do tenant.
/// Herda Outbox, Audit Trail e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>
/// e implementa <see cref="IUnitOfWork"/>. E SEGURANCA CRITICA: as senhas vivem aqui apenas como
/// hash (BCrypt), nunca em claro.
/// </summary>
public sealed class IdentidadeDbContext(DbContextOptions<IdentidadeDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "identidade";

    /// <summary>Usuarios (principais autenticaveis do RBAC) do tenant.</summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Papeis (perfis RBAC) do tenant.</summary>
    public DbSet<Papel> Papeis => Set<Papel>();

    /// <summary>Unidades Organizacionais (arvore de UOs) do tenant.</summary>
    public DbSet<UnidadeOrganizacional> Unidades => Set<UnidadeOrganizacional>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentidadeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
