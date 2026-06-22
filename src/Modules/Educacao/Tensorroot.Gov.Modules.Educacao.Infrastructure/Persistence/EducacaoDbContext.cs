using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;

/// <summary>
/// DbContext do modulo Educacao (schema isolado "educacao"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class EducacaoDbContext(DbContextOptions<EducacaoDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "educacao";

    /// <summary>Escolas (unidades escolares da rede de ensino).</summary>
    public DbSet<Escola> Escolas => Set<Escola>();

    /// <summary>Matriculas (vinculos aluno-turma-escola).</summary>
    public DbSet<Matricula> Matriculas => Set<Matricula>();

    /// <summary>Diarios de classe (frequencia, conteudos e notas por matricula).</summary>
    public DbSet<DiarioClasseAggregate> DiariosClasse => Set<DiarioClasseAggregate>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EducacaoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
