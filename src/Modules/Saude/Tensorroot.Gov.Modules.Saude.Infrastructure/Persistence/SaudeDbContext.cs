using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;
using Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Fiscal;
using AgendamentoRaiz = Tensorroot.Gov.Modules.Saude.Domain.Agendamento.Agendamento;
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

    /// <summary>Estabelecimentos de saude (CNES) — master data local, raiz de agregado.</summary>
    public DbSet<Estabelecimento> Estabelecimentos => Set<Estabelecimento>();

    /// <summary>Profissionais de saude (equipe das unidades) — master data local, raiz de agregado.</summary>
    public DbSet<Profissional> Profissionais => Set<Profissional>();

    /// <summary>Agendas de disponibilidade do profissional (grade → vagas) — raiz de agregado.</summary>
    public DbSet<AgendaProfissional> AgendasProfissional => Set<AgendaProfissional>();

    /// <summary>Agendamentos (marcacoes de consulta/exame) — raiz de agregado.</summary>
    public DbSet<AgendamentoRaiz> Agendamentos => Set<AgendamentoRaiz>();

    /// <summary>Fila de espera por vaga (convocacao por prioridade) — raiz de agregado.</summary>
    public DbSet<FilaEspera> FilasEspera => Set<FilaEspera>();

    /// <summary>Regras de classificacao ASPS versionadas (S-1, LC 141/2012 arts. 3º/4º).</summary>
    public DbSet<RegraClassificacaoAsps> RegrasClassificacaoAsps => Set<RegraClassificacaoAsps>();

    /// <summary>Fundo Municipal de Saude por bloco de financiamento (S-2, Port. 3.992/2017) — raiz de agregado.</summary>
    public DbSet<FundoMunicipalSaude> FundosMunicipaisSaude => Set<FundoMunicipalSaude>();

    /// <summary>Linhas de execucao fiscal de Saude decompostas (S-1 read model, Via A2).</summary>
    public DbSet<LinhaExecucaoSaude> LinhasExecucaoSaude => Set<LinhaExecucaoSaude>();

    /// <summary>Percentuais fiscais de Saude versionados por tenant+vigencia (S-1 — nunca hardcoded).</summary>
    public DbSet<ParametroFiscalSaude> ParametrosFiscaisSaude => Set<ParametroFiscalSaude>();

    /// <summary>Catalogo de medicamentos da rede (REMUME) — raiz de agregado (Onda 3c Farmacia).</summary>
    public DbSet<Medicamento> Medicamentos => Set<Medicamento>();

    /// <summary>Estoque de medicamentos por estabelecimento+medicamento (saldo/lotes/FEFO) — raiz de agregado.</summary>
    public DbSet<EstoqueMedicamento> EstoquesMedicamento => Set<EstoqueMedicamento>();

    /// <summary>Dispensacoes de medicamento ao paciente (LGPD) — raiz de agregado.</summary>
    public DbSet<Dispensacao> Dispensacoes => Set<Dispensacao>();

    /// <summary>Catalogo de imunobiologicos (PNI) — raiz de agregado (Onda 3c Imunizacao).</summary>
    public DbSet<Imunobiologico> Imunobiologicos => Set<Imunobiologico>();

    /// <summary>Carteiras de vacinacao do paciente (doses/aprazamento, LGPD) — raiz de agregado.</summary>
    public DbSet<CarteiraVacinacao> CarteirasVacinacao => Set<CarteiraVacinacao>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SaudeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SincronizarColunasBuscaPaciente();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SincronizarColunasBuscaPaciente();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    // Mantem as colunas-sombra de busca do Paciente sincronizadas com a Identificacao (owned/JSON):
    // NomeBusca normalizado (lowercase + sem diacriticos) e CpfBusca apenas digitos. A busca
    // (PacienteRepository) normaliza o termo do mesmo modo antes do LIKE, garantindo simetria.
    private void SincronizarColunasBuscaPaciente()
    {
        foreach (var entrada in ChangeTracker.Entries<Paciente>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
            {
                entrada.Property("NomeBusca").CurrentValue = BuscaTexto.Normalizar(entrada.Entity.Identificacao.Nome);
                entrada.Property("CpfBusca").CurrentValue = entrada.Entity.Identificacao.Cpf?.Digitos;
            }
        }
    }
}
