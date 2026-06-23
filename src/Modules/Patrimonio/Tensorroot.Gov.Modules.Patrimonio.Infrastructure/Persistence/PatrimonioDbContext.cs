using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Patrimonio (schema isolado "patrimonio"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class PatrimonioDbContext(DbContextOptions<PatrimonioDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "patrimonio";

    /// <summary>Bens patrimoniais (móveis e imóveis).</summary>
    public DbSet<BemPatrimonial> Bens => Set<BemPatrimonial>();

    /// <summary>Veículos da frota pública.</summary>
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    /// <summary>Itens de almoxarifado (estoque).</summary>
    public DbSet<ItemEstoque> ItensEstoque => Set<ItemEstoque>();

    /// <summary>Inventários patrimoniais (Lei 4.320 art. 96).</summary>
    public DbSet<Inventario> Inventarios => Set<Inventario>();

    /// <summary>Pedidos de requisição de almoxarifado self-service (Solicitado→Aprovado→Atendido).</summary>
    public DbSet<PedidoRequisicao> PedidosRequisicao => Set<PedidoRequisicao>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatrimonioDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SincronizarColunasBusca();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SincronizarColunasBusca();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    // Mantem as colunas-sombra de busca (navegabilidade — Onda 0) sincronizadas com os VOs que tem value
    // converter (e por isso nao sao LIKE-aveis diretamente): TombamentoBusca (bem), PlacaBusca/RenavamBusca
    // (veiculo). A busca usa essas colunas; Descricao/Codigo sao strings reais e nao precisam de sombra.
    private void SincronizarColunasBusca()
    {
        foreach (var entrada in ChangeTracker.Entries<BemPatrimonial>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
            {
                entrada.Property("TombamentoBusca").CurrentValue = entrada.Entity.NumeroTombamento?.Valor;
            }
        }

        foreach (var entrada in ChangeTracker.Entries<Veiculo>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
            {
                entrada.Property("PlacaBusca").CurrentValue = entrada.Entity.Placa.Valor;
                entrada.Property("RenavamBusca").CurrentValue = entrada.Entity.Renavam.Digitos;
            }
        }
    }
}
