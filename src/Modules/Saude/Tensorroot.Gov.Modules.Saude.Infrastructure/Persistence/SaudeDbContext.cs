using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;
using AtendimentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo Saude (schema isolado "saude"), herdando Outbox, Audit Trail e o
/// Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// Trata dados pessoais sensiveis (LGPD art. 11) sempre tenant-scoped.
/// </summary>
public sealed class SaudeDbContext(DbContextOptions<SaudeDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "saude";

    /// <summary>Pacientes (PEP/e-SUS APS) — raiz de agregado.</summary>
    public DbSet<Paciente> Pacientes => Set<Paciente>();

    /// <summary>Atendimentos clinicos (PEP) — raiz de agregado.</summary>
    public DbSet<AtendimentoRaiz> Atendimentos => Set<AtendimentoRaiz>();

    /// <summary>Solicitacoes de regulacao (SISREG) — raiz de agregado.</summary>
    public DbSet<SolicitacaoRegulacao> SolicitacoesRegulacao => Set<SolicitacaoRegulacao>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SaudeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
