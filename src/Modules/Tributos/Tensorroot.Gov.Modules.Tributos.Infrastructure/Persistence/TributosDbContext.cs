using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Tributos.Domain.Alvaras;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Certidoes;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Cosip;
using Tensorroot.Gov.Modules.Tributos.Domain.Desif;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Domicilio;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;
using Tensorroot.Gov.Modules.Tributos.Domain.Sim;
using Tensorroot.Gov.Modules.Tributos.Domain.Taxas;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Tributos (schema isolado "tributos"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class TributosDbContext(DbContextOptions<TributosDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "tributos";

    /// <summary>Contribuintes.</summary>
    public DbSet<Contribuinte> Contribuintes => Set<Contribuinte>();

    /// <summary>Lançamentos tributários.</summary>
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

    /// <summary>Dívidas ativas.</summary>
    public DbSet<DividaAtiva> DividasAtivas => Set<DividaAtiva>();

    /// <summary>NFS-e sincronizadas do Ambiente Nacional (read model fiscal — ADR-0003).</summary>
    public DbSet<NotaFiscalServico> NotasFiscaisServico => Set<NotaFiscalServico>();

    /// <summary>Imóveis do cadastro imobiliário (BCI).</summary>
    public DbSet<Imovel> Imoveis => Set<Imovel>();

    /// <summary>Plantas Genéricas de Valores (PGV), versionadas por exercício.</summary>
    public DbSet<PlantaValores> PlantasValores => Set<PlantaValores>();

    /// <summary>Tabelas de alíquotas do IPTU, versionadas por exercício.</summary>
    public DbSet<TabelaAliquotaIptu> TabelasAliquotaIptu => Set<TabelaAliquotaIptu>();

    /// <summary>Documentos de Arrecadação Municipal (guias/carnês).</summary>
    public DbSet<Dam> Dams => Set<Dam>();

    /// <summary>Tabelas de alíquotas do ISS por item LC 116, versionadas por vigência.</summary>
    public DbSet<TabelaAliquotaIss> TabelasAliquotaIss => Set<TabelaAliquotaIss>();

    /// <summary>Apurações mensais do ISS (livro/escrituração eletrônica) por contribuinte/competência.</summary>
    public DbSet<ApuracaoIss> ApuracoesIss => Set<ApuracaoIss>();

    /// <summary>Declarações mensais de ISS (GIA do prestador) por contribuinte/competência.</summary>
    public DbSet<DeclaracaoGiaIss> DeclaracoesGiaIss => Set<DeclaracaoGiaIss>();

    /// <summary>Certidões de regularidade fiscal emitidas (CND/CPEN — CTN 205/206).</summary>
    public DbSet<CertidaoRegularidadeFiscal> CertidoesRegularidadeFiscal => Set<CertidaoRegularidadeFiscal>();

    /// <summary>Alíquotas do ITBI por exercício (lei municipal).</summary>
    public DbSet<AliquotaItbi> AliquotasItbi => Set<AliquotaItbi>();

    /// <summary>Transmissões imobiliárias (fato gerador do ITBI).</summary>
    public DbSet<TransmissaoImobiliaria> TransmissoesImobiliarias => Set<TransmissaoImobiliaria>();

    /// <summary>Processos de arbitramento da base de cálculo do ITBI (CTN art. 148, Tema 1.113/STJ).</summary>
    public DbSet<ProcessoArbitramentoItbi> ProcessosArbitramentoItbi => Set<ProcessoArbitramentoItbi>();

    /// <summary>Tabelas de taxas (poder de polícia/serviço/licença TLL), por código e exercício (lei municipal).</summary>
    public DbSet<TabelaTaxa> TabelasTaxa => Set<TabelaTaxa>();

    /// <summary>Alvarás (atos administrativos de polícia) — a TLL correlata é lançada como Taxa.</summary>
    public DbSet<Alvara> Alvaras => Set<Alvara>();

    /// <summary>Tabelas de COSIP por exercício (faixas de consumo/classe — lei municipal, CF art. 149-A).</summary>
    public DbSet<TabelaCosip> TabelasCosip => Set<TabelaCosip>();

    /// <summary>Obras de Contribuição de Melhoria (edital, impugnação e rateio — CTN arts. 81–82).</summary>
    public DbSet<ObraContribuicaoMelhoria> ObrasContribuicaoMelhoria => Set<ObraContribuicaoMelhoria>();

    /// <summary>Declarações DES-IF (Apuração Mensal do ISSQN das instituições financeiras — ABRASF).</summary>
    public DbSet<DeclaracaoDesif> DeclaracoesDesif => Set<DeclaracaoDesif>();

    /// <summary>Títulos de registro no Serviço de Inspeção Municipal (S.I.M. — produtos de origem animal/vegetal).</summary>
    public DbSet<TituloRegistroSim> TitulosRegistroSim => Set<TituloRegistroSim>();

    /// <summary>Domicílios Eletrônicos do Contribuinte (DEC — caixa postal fiscal com ciência/prazo).</summary>
    public DbSet<DomicilioEletronicoContribuinte> DomiciliosEletronicos => Set<DomicilioEletronicoContribuinte>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TributosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
