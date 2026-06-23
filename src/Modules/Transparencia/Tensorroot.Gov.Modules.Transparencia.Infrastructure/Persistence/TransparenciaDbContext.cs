using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasFolha;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Transparencia (schema isolado "transparencia"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class TransparenciaDbContext(DbContextOptions<TransparenciaDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "transparencia";

    /// <summary>Remessas ao TCE-RS (SIAPC/PAD) — papel CONSUMIDOR.</summary>
    public DbSet<RemessaTce> RemessasTce => Set<RemessaTce>();

    /// <summary>Declaracoes fiscais (MSC/RREO/RGF/DCA) transmitidas ao SICONFI.</summary>
    public DbSet<DeclaracaoFiscal> DeclaracoesFiscais => Set<DeclaracaoFiscal>();

    /// <summary>Resumos de folha consumidos do RH (read model) — fonte da remessa de folha ao TCE-RS.</summary>
    public DbSet<ResumoFolhaTce> ResumosFolhaTce => Set<ResumoFolhaTce>();

    /// <summary>Regras de classificação setorial por função/fonte (núcleo fiscal M7.0.0).</summary>
    public DbSet<FonteRecursoVinculado> FontesRecursoVinculado => Set<FonteRecursoVinculado>();

    /// <summary>Prazos federais/estaduais parametrizáveis (calendário M7.0.1).</summary>
    public DbSet<CalendarioFederal> CalendariosFederais => Set<CalendarioFederal>();

    /// <summary>Pareceres dos conselhos de controle social (M7.0.2).</summary>
    public DbSet<ParecerConselho> ParecesConselho => Set<ParecerConselho>();

    /// <summary>Linhas de execução fiscal decompostas por função/fonte (read model M7.0.0 Via A2).</summary>
    public DbSet<LinhaExecucaoFiscal> LinhasExecucaoFiscal => Set<LinhaExecucaoFiscal>();

    /// <summary>Percentuais mínimos versionados por tenant+vigência (M7.0.3 — nunca hardcoded).</summary>
    public DbSet<ParametroFiscalVigente> ParametrosFiscaisVigentes => Set<ParametroFiscalVigente>();

    /// <summary>Configuração do PORTAL PÚBLICO por tenant (slug + nome de exibição) — Onda 2.</summary>
    public DbSet<PortalPublicoConfig> PortaisPublicos => Set<PortalPublicoConfig>();

    /// <summary>Read model público de DESPESA (transparência ativa, projetado de eventos — I-13).</summary>
    public DbSet<PublicacaoDespesa> PublicacoesDespesa => Set<PublicacaoDespesa>();

    /// <summary>Read model público de RECEITA arrecadada (projetado de eventos — I-13).</summary>
    public DbSet<PublicacaoReceita> PublicacoesReceita => Set<PublicacaoReceita>();

    /// <summary>Read model público de CONTRATO (projetado de eventos de Administração — I-13).</summary>
    public DbSet<PublicacaoContrato> PublicacoesContrato => Set<PublicacaoContrato>();

    /// <summary>Read model público de FOLHA NOMINAL (sem CPF/matrícula; derivado do RH — I-13).</summary>
    public DbSet<PublicacaoFolhaNominal> PublicacoesFolhaNominal => Set<PublicacaoFolhaNominal>();

    /// <summary>Pedidos de informação e-SIC (transparência passiva — LAI Lei 12.527/2011).</summary>
    public DbSet<PedidoInformacaoSic> PedidosInformacaoSic => Set<PedidoInformacaoSic>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransparenciaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
