using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo Legislativo (schema isolado "legislativo"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class LegislativoDbContext(DbContextOptions<LegislativoDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "legislativo";

    /// <summary>Proposicoes (materias submetidas a apreciacao do Plenario).</summary>
    public DbSet<Proposicao> Proposicoes => Set<Proposicao>();

    /// <summary>Sessoes plenarias (ordinarias e extraordinarias).</summary>
    public DbSet<Sessao> Sessoes => Set<Sessao>();

    /// <summary>Votacoes (deliberacoes do Plenario).</summary>
    public DbSet<Votacao> Votacoes => Set<Votacao>();

    /// <summary>Vereadores (cadastro de parlamentares da Camara).</summary>
    public DbSet<Vereador> Vereadores => Set<Vereador>();

    /// <summary>Normas juridicas (base consultavel de leis/decretos/resolucoes) — G1.</summary>
    public DbSet<Norma> Normas => Set<Norma>();

    /// <summary>Edicoes do Diario Oficial Eletronico — G2.</summary>
    public DbSet<EdicaoDiario> DiarioEdicoes => Set<EdicaoDiario>();

    /// <summary>Tribunas de sessao (inscricao de oradores + cronometro) — G3.</summary>
    public DbSet<TribunaSessao> Tribunas => Set<TribunaSessao>();

    /// <summary>Comissoes (permanentes/temporarias) com composicao e presidencia — G4.</summary>
    public DbSet<Comissao> Comissoes => Set<Comissao>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LegislativoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SincronizarColunaBuscaNorma();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SincronizarColunaBuscaNorma();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    // Mantem a coluna-sombra "EmentaBusca" (indexada) sincronizada com a ementa, JA NORMALIZADA
    // (lowercase + sem diacriticos — BUG-3), para busca textual independente de collation. A busca
    // (NormaRepository) normaliza o termo do mesmo modo antes do LIKE, garantindo simetria.
    private void SincronizarColunaBuscaNorma()
    {
        foreach (var entrada in ChangeTracker.Entries<Norma>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
            {
                entrada.Property("EmentaBusca").CurrentValue = TextoBusca.Normalizar(entrada.Entity.Ementa.Valor);
            }
        }
    }
}
