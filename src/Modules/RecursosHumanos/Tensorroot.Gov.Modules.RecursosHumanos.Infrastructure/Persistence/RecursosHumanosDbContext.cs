using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Pasep;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo RecursosHumanos (schema isolado "recursoshumanos"), herdando Outbox,
/// Audit Trail e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa
/// <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class RecursosHumanosDbContext(DbContextOptions<RecursosHumanosDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "recursoshumanos";

    /// <summary>Servidores (vinculos de pessoal: estatutarios RPPS e celetistas RGPS).</summary>
    public DbSet<Servidor> Servidores => Set<Servidor>();

    /// <summary>Cargos publicos da estrutura de pessoal.</summary>
    public DbSet<Cargo> Cargos => Set<Cargo>();

    /// <summary>Vinculos usuario&#8596;servidor (ancora do autosservico "Minha Folha").</summary>
    public DbSet<VinculoServidorUsuario> VinculosServidorUsuario => Set<VinculoServidorUsuario>();

    /// <summary>Folhas de pagamento por competencia.</summary>
    public DbSet<FolhaDePagamento> FolhasDePagamento => Set<FolhaDePagamento>();

    /// <summary>Rubricas (verbas) parametrizaveis do catalogo do tenant (S-1010).</summary>
    public DbSet<RubricaFolha> Rubricas => Set<RubricaFolha>();

    /// <summary>Tabelas INSS (RGPS) parametrizadas por competencia.</summary>
    public DbSet<TabelaInss> TabelasInss => Set<TabelaInss>();

    /// <summary>Tabelas IRRF parametrizadas por competencia.</summary>
    public DbSet<TabelaIrrf> TabelasIrrf => Set<TabelaIrrf>();

    /// <summary>Tabelas RPPS municipais parametrizadas por competencia.</summary>
    public DbSet<TabelaRpps> TabelasRpps => Set<TabelaRpps>();

    /// <summary>Marcacoes de ponto (AFD append-only — Portaria MTP 671/2021).</summary>
    public DbSet<MarcacaoPonto> PontoMarcacoes => Set<MarcacaoPonto>();

    /// <summary>Jornadas/escalas de trabalho dos servidores.</summary>
    public DbSet<JornadaTrabalho> PontoJornadas => Set<JornadaTrabalho>();

    /// <summary>Apuracoes de jornada por servidor/competencia (PTRP/banco de horas).</summary>
    public DbSet<ApuracaoPonto> PontoApuracoes => Set<ApuracaoPonto>();

    /// <summary>Bancos de horas (saldo vivo por servidor + livro-razao de creditos/debitos/prescricao).</summary>
    public DbSet<BancoDeHoras> BancosDeHoras => Set<BancoDeHoras>();

    /// <summary>Parque de equipamentos REP cadastrados (coleta de AFD do hardware — Port. 671).</summary>
    public DbSet<RepConfigurado> PontoReps => Set<RepConfigurado>();

    /// <summary>Eventos eSocial gerados/assinados/transmitidos (maquina de estados + Outbox).</summary>
    public DbSet<EventoESocial> EventosESocial => Set<EventoESocial>();

    /// <summary>Afastamentos/licencas tipados dos servidores (efeito determinIstico na folha).</summary>
    public DbSet<Afastamento> Afastamentos => Set<Afastamento>();

    /// <summary>Regras legais de afastamento parametrizaveis por tenant/vigencia (fonte do efeito na folha).</summary>
    public DbSet<RegraAfastamento> RegrasAfastamento => Set<RegraAfastamento>();

    /// <summary>Cadastro mestre de consignatarias (bancos/entidades habilitadas — Lei 14.131/2021).</summary>
    public DbSet<Consignataria> Consignatarias => Set<Consignataria>();

    /// <summary>Catalogo parametrizavel de rubricas consignaveis (categoria/balde de margem por tenant).</summary>
    public DbSet<RubricaConsignavel> RubricasConsignaveis => Set<RubricaConsignavel>();

    /// <summary>Contratos de consignacao dos servidores (efeito determinIstico na folha respeitando a margem).</summary>
    public DbSet<ContratoConsignacao> ContratosConsignacao => Set<ContratoConsignacao>();

    /// <summary>Percentuais de margem consignavel parametrizaveis por tenant/vigencia (35%+5%+5% default legal).</summary>
    public DbSet<ParametrosMargemVigente> ParametrosMargem => Set<ParametrosMargemVigente>();

    /// <summary>Portarias / atos de pessoal (numeracao sequencial por exercicio/tenant).</summary>
    public DbSet<Portaria> Portarias => Set<Portaria>();

    /// <summary>Apuracoes do PASEP por competencia (base = folha bruta; transmissao = M10).</summary>
    public DbSet<ApuracaoPasep> ApuracoesPasep => Set<ApuracaoPasep>();

    /// <summary>ASO (exames ocupacionais) — Saude Ocupacional (S-2220/PCMSO).</summary>
    public DbSet<ExameOcupacional> SstExamesOcupacionais => Set<ExameOcupacional>();

    /// <summary>Periodos de exposicao a agentes nocivos — condicoes ambientais (S-2240/PPP).</summary>
    public DbSet<ExposicaoAgenteNocivo> SstExposicoesAgenteNocivo => Set<ExposicaoAgenteNocivo>();

    /// <summary>Comunicacoes de Acidente de Trabalho (CAT — S-2210).</summary>
    public DbSet<ComunicacaoAcidente> SstComunicacoesAcidente => Set<ComunicacaoAcidente>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecursosHumanosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
