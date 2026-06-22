using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
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

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LegislativoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
