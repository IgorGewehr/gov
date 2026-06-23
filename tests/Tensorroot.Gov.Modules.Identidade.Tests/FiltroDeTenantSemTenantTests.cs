using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Xunit;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// XT-2 (RED-TEAM): o Global Query Filter NÃO pode DEGRADAR para <c>TenantId == Guid.Empty</c> quando
/// não há tenant resolvido — isso filtraria por "tenant zero" e devolveria linhas órfãs, quebrando o
/// deny-by-default (CLAUDE.md §5). Sem tenant resolvido (e fora do contexto de SISTEMA), o predicado
/// deve NEGAR (1=0). Para o contexto de SISTEMA (DDL/migração/seed) o filtro é passe-livre.
/// </summary>
public sealed class FiltroDeTenantSemTenantTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UoSaude = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly SqliteConnection _connection;

    public FiltroDeTenantSemTenantTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Semear();
    }

    private ContextoDeTeste CriarContexto(ITenantContext tenant)
    {
        var options = new DbContextOptionsBuilder<ContextoDeTeste>().UseSqlite(_connection).Options;
        var contexto = new ContextoDeTeste(options, tenant, unidade: null);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    private void Semear()
    {
        using var seed = CriarContexto(new TenantFixoA(TenantA));
        seed.Recursos.Add(new RecursoComUnidade(Guid.NewGuid(), TenantA, UoSaude, "Empenho Saude"));
        seed.Globais.Add(new RecursoSemUnidade(Guid.NewGuid(), TenantA, "Parametro global"));
        seed.SaveChanges();
    }

    [Fact]
    public void Sem_tenant_resolvido_o_filtro_NEGA_em_vez_de_filtrar_por_Guid_Empty()
    {
        // Contexto com principal SEM tenant (não-sistema): NUNCA deve "virar tenant zero".
        using var contexto = CriarContexto(new SemTenant());

        // Deny-by-default: nenhuma linha tenant-scoped é visível (nem as órfãs de Guid.Empty).
        contexto.Recursos.Should().BeEmpty();
        contexto.Globais.Should().BeEmpty();
    }

    [Fact]
    public void Sem_tenant_resolvido_nao_vaza_linha_orfa_de_tenant_zero()
    {
        // Insere uma linha órfã com TenantId == Guid.Empty pela via de SISTEMA (passe-livre).
        using (var sistema = CriarContexto(SistemaTenantContext.Instancia))
        {
            sistema.Recursos.Add(new RecursoComUnidade(Guid.NewGuid(), Guid.Empty, UoSaude, "Orfao tenant-zero"));
            sistema.SaveChanges();
        }

        // O caminho sem tenant NÃO deve enxergar a órfã (antes do fix, Guid.Empty == Guid.Empty vazava).
        using var contexto = CriarContexto(new SemTenant());
        contexto.Recursos.Should().BeEmpty();
    }

    [Fact]
    public void Contexto_de_SISTEMA_e_passe_livre_para_DDL_seed()
    {
        // Bootstrap/seed: enxerga tudo (não nega, não filtra por Guid.Empty).
        using var contexto = CriarContexto(SistemaTenantContext.Instancia);

        contexto.Recursos.Should().ContainSingle().Which.Descricao.Should().Be("Empenho Saude");
        contexto.Globais.Should().ContainSingle();
    }

    [Fact]
    public void Com_tenant_resolvido_o_isolamento_normal_continua_valendo()
    {
        using var contexto = CriarContexto(new TenantFixoA(TenantA));

        contexto.Recursos.Should().ContainSingle().Which.Descricao.Should().Be("Empenho Saude");
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TenantFixoA(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;

        public bool HasTenant => true;
    }

    // Principal autenticado mas SEM tenant resolvido (NÃO é o contexto de sistema).
    private sealed class SemTenant : ITenantContext
    {
        public Guid TenantId => throw new InvalidOperationException("Sem tenant.");

        public bool HasTenant => false;
    }
}
