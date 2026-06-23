using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.Receitas;
using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;

/// <summary>DbContext do módulo Finanças (schema isolado "financas"), com Outbox, auditoria e filtro de tenant.</summary>
public sealed class FinancasDbContext(DbContextOptions<FinancasDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "financas";

    /// <summary>Dotações orçamentárias (créditos da LOA).</summary>
    public DbSet<DotacaoOrcamentaria> Dotacoes => Set<DotacaoOrcamentaria>();

    /// <summary>Empenhos.</summary>
    public DbSet<Empenho> Empenhos => Set<Empenho>();

    /// <summary>Liquidações de despesa.</summary>
    public DbSet<Liquidacao> Liquidacoes => Set<Liquidacao>();

    /// <summary>Ordens de pagamento.</summary>
    public DbSet<OrdemDePagamento> OrdensDePagamento => Set<OrdemDePagamento>();

    /// <summary>Restos a Pagar.</summary>
    public DbSet<RestoAPagar> RestosAPagar => Set<RestoAPagar>();

    /// <summary>Receitas arrecadadas (recebidas via integração de outros módulos).</summary>
    public DbSet<ReceitaArrecadada> ReceitasArrecadadas => Set<ReceitaArrecadada>();

    /// <summary>Plano de contas PCASP (contas contábeis por tenant).</summary>
    public DbSet<ContaContabil> ContasContabeis => Set<ContaContabil>();

    /// <summary>Lançamentos contábeis (partidas dobradas).</summary>
    public DbSet<LancamentoContabil> LancamentosContabeis => Set<LancamentoContabil>();

    /// <summary>Eventos contábeis (roteiros parametrizáveis).</summary>
    public DbSet<EventoContabil> EventosContabeis => Set<EventoContabil>();

    /// <summary>Projeção do balancete (read model de saldos por conta/período).</summary>
    public DbSet<LinhaBalancete> BalancetesConta => Set<LinhaBalancete>();

    /// <summary>Registros de controle de geração da MSC (idempotência por competência).</summary>
    public DbSet<MscGeradaRegistro> MscsGeradas => Set<MscGeradaRegistro>();

    /// <summary>Planos Plurianuais (PPA — planejamento de 4 anos).</summary>
    public DbSet<PlanoPlurianual> Ppas => Set<PlanoPlurianual>();

    /// <summary>Leis de Diretrizes Orçamentárias (LDO).</summary>
    public DbSet<LeiDiretrizes> Ldos => Set<LeiDiretrizes>();

    /// <summary>Leis Orçamentárias Anuais (LOA + QDD).</summary>
    public DbSet<LeiOrcamentariaAnual> Loas => Set<LeiOrcamentariaAnual>();

    /// <summary>Créditos adicionais (alteram a LOA).</summary>
    public DbSet<CreditoAdicional> CreditosAdicionais => Set<CreditoAdicional>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinancasDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
