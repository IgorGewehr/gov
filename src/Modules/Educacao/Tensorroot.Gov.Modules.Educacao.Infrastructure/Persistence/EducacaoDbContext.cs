using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Fiscal;

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

    /// <summary>Regras de classificacao MDE versionadas (E-1, CF art. 212 / LDB arts. 70/71).</summary>
    public DbSet<RegraClassificacaoMde> RegrasClassificacaoMde => Set<RegraClassificacaoMde>();

    /// <summary>Distribuicao do FUNDEB por origem (E-3, Lei 14.113/2020) — raiz de agregado.</summary>
    public DbSet<DistribuicaoFundeb> DistribuicoesFundeb => Set<DistribuicaoFundeb>();

    /// <summary>Linhas de execucao fiscal de Educacao decompostas (E-1 read model, Via A2).</summary>
    public DbSet<LinhaExecucaoEducacao> LinhasExecucaoEducacao => Set<LinhaExecucaoEducacao>();

    /// <summary>Percentuais fiscais de Educacao versionados por tenant+vigencia (E-1/E-2 — nunca hardcoded).</summary>
    public DbSet<ParametroFiscalEducacao> ParametrosFiscaisEducacao => Set<ParametroFiscalEducacao>();

    /// <summary>Remuneracao dos profissionais da educacao por exercicio (E-2, numerador dos 70%).</summary>
    public DbSet<RemuneracaoMagisterioExercicio> RemuneracoesMagisterio => Set<RemuneracaoMagisterioExercicio>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EducacaoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
