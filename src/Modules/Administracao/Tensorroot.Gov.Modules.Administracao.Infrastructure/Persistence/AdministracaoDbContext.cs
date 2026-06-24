using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.Pca;
using Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo Administracao (schema isolado "administracao"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class AdministracaoDbContext(DbContextOptions<AdministracaoDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "administracao";

    /// <summary>Licitacoes (Lei 14.133/2021).</summary>
    public DbSet<Licitacao> Licitacoes => Set<Licitacao>();

    /// <summary>Contratos administrativos.</summary>
    public DbSet<Contrato> Contratos => Set<Contrato>();

    /// <summary>Fornecedores aptos a contratar.</summary>
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();

    /// <summary>Catalogo de materiais e servicos padronizados (CATMAT/CATSER).</summary>
    public DbSet<ItemCatalogo> CatalogoItens => Set<ItemCatalogo>();

    /// <summary>Atas de Registro de Precos (ARP — art. 82-86, Lei 14.133/2021).</summary>
    public DbSet<Ata> Atas => Set<Ata>();

    /// <summary>Planos de Contratacoes Anuais (PCA — art. 12, VII, Lei 14.133/2021).</summary>
    public DbSet<PlanoContratacoes> PlanosContratacoes => Set<PlanoContratacoes>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdministracaoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
