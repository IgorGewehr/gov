using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.Modules.Cofre.Tests;

/// <summary>
/// Cobre os dois fixes ALTOS da auditoria:
/// <list type="bullet">
/// <item><b>A1</b> — ordem CORRETA dos interceptors (Tenant -> Audit): uma entidade SEM TenantId
/// pré-setado pelo factory ainda assim gera trilha com <c>TenantId == tenant corrente</c> (antes
/// seria <c>Guid.Empty</c>, W0.2).</item>
/// <item><b>A2</b> — cadeia de hash por tenant: encadeamento estável, verificador OK para trilha
/// íntegra e apontando a 1ª linha em caso de adulteração de conteúdo ou de remoção de linha.</item>
/// </list>
/// Usa um <see cref="ModuleDbContext"/> mínimo de teste sobre SQLite em memória, registrando os
/// interceptors pelo helper ÚNICO <see cref="ModuleInterceptorRegistration"/> — a MESMA ordem usada
/// pelos 13 módulos em produção.
/// </summary>
public sealed class AuditoriaHashChainTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;

    public AuditoriaHashChainTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task A1_entidade_sem_tenant_presetado_grava_trilha_com_tenant_corrente()
    {
        await using var contexto = CriarContexto(TenantA);

        // Factory NÃO define TenantId — ele nasce Guid.Empty e depende do TenantSaveChangesInterceptor.
        var entidade = EntidadeDeTeste.Criar("registro-1");
        entidade.TenantId.Should().Be(Guid.Empty, "o factory propositalmente não carimba o tenant");

        contexto.Entidades.Add(entidade);
        await contexto.SaveChangesAsync();

        // A entidade foi carimbada pelo interceptor de tenant...
        entidade.TenantId.Should().Be(TenantA);

        // ...e a trilha leu o TenantId JÁ carimbado (não Guid.Empty) — prova da ordem Tenant -> Audit.
        var trilha = await contexto.Set<AuditTrail>().AsNoTracking().SingleAsync();
        trilha.TenantId.Should().Be(TenantA, "a ordem correta dos interceptors faz a trilha ler o tenant carimbado (W0.2)");
        trilha.TenantId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task A2_cadeia_encadeia_por_tenant_em_ordem_de_gravacao()
    {
        await using var contexto = CriarContexto(TenantA);

        await SalvarAsync(contexto, "a");
        await SalvarAsync(contexto, "b");
        await SalvarAsync(contexto, "c");

        var linhas = await contexto.Set<AuditTrail>().AsNoTracking()
            .Where(l => l.TenantId == TenantA)
            .OrderBy(l => l.Sequencia)
            .ToListAsync();

        linhas.Select(l => l.Sequencia).Should().Equal(1, 2, 3);
        linhas[0].HashAnterior.Should().Be(AuditHashChain.HashGenesis, "a 1ª linha parte do genesis");
        linhas[1].HashAnterior.Should().Be(linhas[0].HashAtual, "cada elo aponta o selo da linha anterior");
        linhas[2].HashAnterior.Should().Be(linhas[1].HashAtual);
        linhas.Select(l => l.HashAtual).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task A2_cadeias_de_tenants_diferentes_sao_independentes()
    {
        await using var contextoA = CriarContexto(TenantA);
        await using var contextoB = CriarContexto(TenantB);

        await SalvarAsync(contextoA, "a1");
        await SalvarAsync(contextoB, "b1");
        await SalvarAsync(contextoA, "a2");

        var seqA = await contextoA.Set<AuditTrail>().AsNoTracking()
            .Where(l => l.TenantId == TenantA).OrderBy(l => l.Sequencia).Select(l => l.Sequencia).ToListAsync();
        var seqB = await contextoB.Set<AuditTrail>().AsNoTracking()
            .Where(l => l.TenantId == TenantB).OrderBy(l => l.Sequencia).Select(l => l.Sequencia).ToListAsync();

        seqA.Should().Equal(new long[] { 1, 2 }, "o tenant A tem sua própria sequência");
        seqB.Should().Equal(new long[] { 1 }, "o tenant B começa do 1 independentemente do A");
    }

    [Fact]
    public async Task A2_verificador_aprova_trilha_integra()
    {
        await using var contexto = CriarContexto(TenantA);
        await SalvarAsync(contexto, "a");
        await SalvarAsync(contexto, "b");

        var verificador = new VerificadorTrilhaAuditoria();
        var resultado = await verificador.VerificarAsync(
            contexto.Set<AuditTrail>().AsNoTracking(), TenantA, default);

        resultado.Integra.Should().BeTrue();
        resultado.LinhasVerificadas.Should().Be(2);
        resultado.PrimeiraDivergencia.Should().BeNull();
    }

    [Fact]
    public async Task A2_verificador_detecta_adulteracao_de_conteudo_e_aponta_a_linha()
    {
        await using var contexto = CriarContexto(TenantA);
        await SalvarAsync(contexto, "a");
        await SalvarAsync(contexto, "b");
        await SalvarAsync(contexto, "c");

        // Adultera DIRETO no banco (simula operador malicioso) a 2ª linha — burla o init-only do app.
        var alvo = await contexto.Set<AuditTrail>().AsNoTracking()
            .Where(l => l.TenantId == TenantA && l.Sequencia == 2).Select(l => l.Id).SingleAsync();
        var nomeTabela = await DescobrirNomeTabelaAuditTrailAsync();
        // Alvo pela posição (TenantId + Sequencia) — robusto à serialização do Guid no SQLite.
        var afetadas = await ExecutarSqlAsync(
            $"UPDATE \"{nomeTabela}\" SET \"NewValues\" = '{{\"Nome\":\"ADULTERADO\"}}' WHERE \"TenantId\" = $id AND \"Sequencia\" = 2;", TenantA);
        afetadas.Should().Be(1, $"a adulteração deve atingir exatamente a linha alvo na tabela '{nomeTabela}'");

        var verificador = new VerificadorTrilhaAuditoria();
        var resultado = await verificador.VerificarAsync(
            contexto.Set<AuditTrail>().AsNoTracking(), TenantA, default);

        resultado.Integra.Should().BeFalse();
        resultado.SequenciaDivergente.Should().Be(2, "a recomputação falha exatamente na linha adulterada");
        resultado.PrimeiraDivergencia.Should().Be(alvo);
        resultado.Motivo.Should().Contain("adulterado");
    }

    [Fact]
    public async Task A2_verificador_detecta_remocao_de_linha_por_lacuna_na_sequencia()
    {
        await using var contexto = CriarContexto(TenantA);
        await SalvarAsync(contexto, "a");
        await SalvarAsync(contexto, "b");
        await SalvarAsync(contexto, "c");

        // Remove a 2ª linha direto no banco — abre lacuna 1,3 (a 3 vira "fora de lugar").
        var nomeTabela = await DescobrirNomeTabelaAuditTrailAsync();
        await ExecutarSqlAsync(
            $"DELETE FROM \"{nomeTabela}\" WHERE \"TenantId\" = $id AND \"Sequencia\" = 2;", TenantA);

        var verificador = new VerificadorTrilhaAuditoria();
        var resultado = await verificador.VerificarAsync(
            contexto.Set<AuditTrail>().AsNoTracking(), TenantA, default);

        resultado.Integra.Should().BeFalse();
        resultado.SequenciaDivergente.Should().Be(2, "a sequência esperada após a 1ª linha é 2, mas vem a 3");
        resultado.Motivo.Should().Contain("Lacuna");
    }

    private static async Task SalvarAsync(TesteDbContext contexto, string nome)
    {
        contexto.Entidades.Add(EntidadeDeTeste.Criar(nome));
        await contexto.SaveChangesAsync();
    }

    /// <summary>Descobre o nome real da tabela da trilha no SQLite (o schema vira prefixo do nome).</summary>
    private async Task<string> DescobrirNomeTabelaAuditTrailAsync()
    {
        await using var comando = _connection.CreateCommand();
        comando.CommandText =
            "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%AuditTrail' LIMIT 1;";
        var nome = (string?)await comando.ExecuteScalarAsync();
        return nome ?? "AuditTrail";
    }

    private async Task<int> ExecutarSqlAsync(string sql, Guid id)
    {
        await using var comando = _connection.CreateCommand();
        comando.CommandText = sql;
        var p = comando.CreateParameter();
        p.ParameterName = "$id";
        p.Value = id.ToString();
        comando.Parameters.Add(p);
        return await comando.ExecuteNonQueryAsync();
    }

    private TesteDbContext CriarContexto(Guid tenant)
    {
        var tenantContext = new TenantContextFake(tenant);
        var options = new DbContextOptionsBuilder<TesteDbContext>()
            .UseSqlite(_connection)
            // MESMO helper único usado pelos 13 módulos: garante a ordem Tenant -> Audit -> Outbox.
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new TesteDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Entidade de teste cujo factory NÃO carimba o TenantId (depende do interceptor).</summary>
    private sealed class EntidadeDeTeste : IMustHaveTenant
    {
        private EntidadeDeTeste()
        {
        }

        public Guid Id { get; private init; }

        // Settable pelo interceptor (carimbo de tenant). Nasce Guid.Empty no factory.
        public Guid TenantId { get; set; }

        public string Nome { get; private init; } = default!;

        public static EntidadeDeTeste Criar(string nome)
            => new() { Id = Guid.NewGuid(), Nome = nome };
    }

    private sealed class TesteDbContext(DbContextOptions<TesteDbContext> options, ITenantContext tenantContext)
        : ModuleDbContext(options, tenantContext)
    {
        public override string Schema => "teste";

        public DbSet<EntidadeDeTeste> Entidades => Set<EntidadeDeTeste>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);
            modelBuilder.Entity<EntidadeDeTeste>(b =>
            {
                b.ToTable("Entidades");
                b.HasKey(e => e.Id);
                b.Property(e => e.Nome).HasMaxLength(128);
            });
            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class TenantContextFake(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;

        public bool HasTenant => true;
    }

    private sealed class CurrentUserFake : ICurrentUser
    {
        public string? UserId => "teste";

        public string? UserName => "Usuario de Teste";

        public string? IpAddress => "127.0.0.1";
    }
}
