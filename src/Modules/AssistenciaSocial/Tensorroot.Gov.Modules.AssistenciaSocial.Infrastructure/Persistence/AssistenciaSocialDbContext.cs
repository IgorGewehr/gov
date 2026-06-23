using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Lookups;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Parametros;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo AssistenciaSocial (schema isolado "assistenciasocial"), herdando Outbox,
/// Audit Trail e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa
/// <see cref="IUnitOfWork"/>. Os dados socioassistenciais sao sensiveis (LGPD art. 11) e nunca
/// cruzam tenants (I-9).
/// </summary>
public sealed class AssistenciaSocialDbContext(DbContextOptions<AssistenciaSocialDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "assistenciasocial";

    /// <summary>Familias referenciadas no SUAS (raiz de agregado).</summary>
    public DbSet<Familia> Familias => Set<Familia>();

    /// <summary>Beneficios socioassistenciais (BPC/PBF/eventuais).</summary>
    public DbSet<Beneficio> Beneficios => Set<Beneficio>();

    /// <summary>Prontuarios SUAS (acompanhamento familiar sigiloso).</summary>
    public DbSet<ProntuarioSuas> Prontuarios => Set<ProntuarioSuas>();

    /// <summary>Projecao das Unidades de Atendimento (CRAS/CREAS/Centro POP) do tenant — read model de apoio.</summary>
    public DbSet<UnidadeAtendimentoLookup> UnidadesAtendimento => Set<UnidadeAtendimentoLookup>();

    /// <summary>Parametros versionados por vigencia (ex.: salario minimo), parametrizaveis por tenant.</summary>
    public DbSet<ParametroVigente> ParametrosVigentes => Set<ParametroVigente>();

    /// <summary>A-0: criterios MUNICIPAIS de beneficio eventual versionados (sem teto federal de 1/4 SM revogado).</summary>
    public DbSet<CriterioBeneficioEventualMunicipal> CriteriosBeneficioEventual => Set<CriterioBeneficioEventualMunicipal>();

    /// <summary>A-1: Fundo Municipal de Assistencia Social (FMAS) — execucao segregada por bloco/piso SUAS.</summary>
    public DbSet<FundoMunicipalAssistencia> FundosMunicipaisAssistencia => Set<FundoMunicipalAssistencia>();

    /// <summary>A-2: Registros Mensais de Atendimentos (RMA) consolidados do Prontuario SUAS.</summary>
    public DbSet<RegistroMensalAtendimento> RegistrosMensaisAtendimento => Set<RegistroMensalAtendimento>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssistenciaSocialDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
