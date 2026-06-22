using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Prova o FILTRO DE LEITURA POR UO (MODELO §5) aplicado por reflexao sobre <see cref="IMustHaveUnidade"/>
/// no <see cref="ModuleDbContext"/> — analogo ao filtro de tenant. Verifica que:
/// (a) com sujeito (DeveFiltrarPorUnidade=true) so as UOs do escopo sao visiveis;
/// (b) sem sujeito (jobs) NAO filtra (igual ao override de tenant);
/// (c) entidades SEM IMustHaveUnidade jamais sao filtradas;
/// (d) o filtro de tenant continua valendo (combinado por AND).
/// </summary>
public sealed class FiltroDeUnidadeTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UoSaude = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UoEducacao = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly SqliteConnection _connection;

    public FiltroDeUnidadeTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    private ContextoDeTeste CriarContexto(ITenantContext tenant, ITenantUnidadeContext? unidade)
    {
        var options = new DbContextOptionsBuilder<ContextoDeTeste>()
            .UseSqlite(_connection)
            .Options;
        var contexto = new ContextoDeTeste(options, tenant, unidade);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    private void Semear()
    {
        // Semeia ignorando filtros (tenant fixo, sem filtro de UO).
        using var seed = CriarContexto(new TenantFixo(TenantA), unidade: null);
        seed.Recursos.Add(new RecursoComUnidade(Guid.NewGuid(), TenantA, UoSaude, "Empenho Saude"));
        seed.Recursos.Add(new RecursoComUnidade(Guid.NewGuid(), TenantA, UoEducacao, "Empenho Educacao"));
        seed.Globais.Add(new RecursoSemUnidade(Guid.NewGuid(), TenantA, "Parametro global"));
        seed.SaveChanges();
    }

    [Fact]
    public void Com_sujeito_so_ve_recursos_das_UOs_do_seu_escopo()
    {
        Semear();

        // Escopo do sujeito: apenas a UO Saude.
        using var contexto = CriarContexto(new TenantFixo(TenantA), new UnidadeFixa(filtrar: true, UoSaude));

        var visiveis = contexto.Recursos.Select(recurso => recurso.Descricao).ToList();

        visiveis.Should().ContainSingle().Which.Should().Be("Empenho Saude");
    }

    [Fact]
    public void Sem_sujeito_job_nao_filtra_por_UO()
    {
        Semear();

        // unidade=null (jobs/sistema): nao filtra por UO (igual ao TenantOverride).
        using var contexto = CriarContexto(new TenantFixo(TenantA), unidade: null);

        contexto.Recursos.Should().HaveCount(2);
    }

    [Fact]
    public void DeveFiltrar_false_nao_filtra_mesmo_com_contexto_de_unidade_presente()
    {
        Semear();

        using var contexto = CriarContexto(new TenantFixo(TenantA), new UnidadeFixa(filtrar: false));

        contexto.Recursos.Should().HaveCount(2);
    }

    [Fact]
    public void Entidade_sem_IMustHaveUnidade_nunca_e_filtrada_por_UO()
    {
        Semear();

        // Mesmo com escopo restrito a Saude, a entidade global (sem IMustHaveUnidade) continua visivel.
        using var contexto = CriarContexto(new TenantFixo(TenantA), new UnidadeFixa(filtrar: true, UoSaude));

        contexto.Globais.Should().ContainSingle().Which.Descricao.Should().Be("Parametro global");
    }

    [Fact]
    public void Escopo_vazio_com_filtro_ligado_nega_tudo_deny_by_default()
    {
        Semear();

        using var contexto = CriarContexto(new TenantFixo(TenantA), new UnidadeFixa(filtrar: true));

        contexto.Recursos.Should().BeEmpty();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TenantFixo(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;

        public bool HasTenant => true;
    }

    private sealed class UnidadeFixa(bool filtrar, params Guid[] unidades) : ITenantUnidadeContext
    {
        public bool DeveFiltrarPorUnidade => filtrar;

        public IReadOnlyCollection<Guid> UnidadesPermitidas => unidades;
    }
}

/// <summary>Entidade de teste com UO dona (alvo do filtro de UO).</summary>
internal sealed class RecursoComUnidade(Guid id, Guid tenantId, Guid unidadeId, string descricao)
    : IMustHaveTenant, IMustHaveUnidade
{
    public Guid Id { get; private set; } = id;

    public Guid TenantId { get; private set; } = tenantId;

    public Guid UnidadeId { get; private set; } = unidadeId;

    public string Descricao { get; private set; } = descricao;
}

/// <summary>Entidade de teste SEM UO (nunca filtrada por UO — compatibilidade).</summary>
internal sealed class RecursoSemUnidade(Guid id, Guid tenantId, string descricao) : IMustHaveTenant
{
    public Guid Id { get; private set; } = id;

    public Guid TenantId { get; private set; } = tenantId;

    public string Descricao { get; private set; } = descricao;
}

/// <summary>DbContext de teste sobre o <see cref="ModuleDbContext"/> base (herda os filtros globais).</summary>
internal sealed class ContextoDeTeste(
    DbContextOptions<ContextoDeTeste> options,
    ITenantContext tenant,
    ITenantUnidadeContext? unidade)
    : ModuleDbContext(options, tenant, holder: null, unidade)
{
    public override string Schema => "teste";

    public DbSet<RecursoComUnidade> Recursos => Set<RecursoComUnidade>();

    public DbSet<RecursoSemUnidade> Globais => Set<RecursoSemUnidade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<RecursoComUnidade>(builder =>
        {
            builder.ToTable("Recursos");
            builder.HasKey(recurso => recurso.Id);
        });
        modelBuilder.Entity<RecursoSemUnidade>(builder =>
        {
            builder.ToTable("Globais");
            builder.HasKey(recurso => recurso.Id);
        });
        base.OnModelCreating(modelBuilder);
    }
}
