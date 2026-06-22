using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova de ponta-a-ponta da persistência do módulo Tributos sobre SQLite em memória:
/// fluxo de Dívida Ativa, gravação de Audit Trail e ISOLAMENTO entre tenants.
/// </summary>
public sealed class DividaAtivaFluxoTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;

    public DividaAtivaFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Fluxo_de_divida_ativa_persiste_com_auditoria_e_respeita_isolamento_de_tenant()
    {
        // Arrange + Act — Tenant A: cadastra, lança, inscreve em dívida ativa e emite a CDA.
        await using (var contexto = CriarContexto(TenantA))
        {
            var contribuinte = Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "Maria Contribuinte");
            contexto.Contribuintes.Add(contribuinte);

            var lancamento = Lancamento.Lancar(
                TenantA,
                contribuinte.Id,
                TipoTributo.Iptu,
                Competencia.De(2024, 1),
                ValorMonetario.De(1500.00m),
                new DateOnly(2024, 3, 10));
            contexto.Lancamentos.Add(lancamento);
            await contexto.SaveChangesAsync();

            lancamento.InscreverEmDividaAtiva(new DateOnly(2026, 6, 20));
            var divida = DividaAtiva.Inscrever(TenantA, contribuinte.Id, lancamento.Id, lancamento.ValorPrincipal, new DateOnly(2026, 6, 20));
            contexto.DividasAtivas.Add(divida);
            await contexto.SaveChangesAsync();

            divida.EmitirCda("CDA-2026/000123");
            await contexto.SaveChangesAsync();
        }

        // Assert — Tenant A lê de volta o estado persistido + a trilha de auditoria.
        await using (var contexto = CriarContexto(TenantA))
        {
            var dividas = await contexto.DividasAtivas.ToListAsync();
            dividas.Should().HaveCount(1);
            dividas[0].Situacao.Should().Be(SituacaoDividaAtiva.CdaEmitida);
            dividas[0].NumeroCda.Should().Be("CDA-2026/000123");
            dividas[0].ValorInscrito.Valor.Should().Be(1500.00m);
            dividas[0].TenantId.Should().Be(TenantA);

            var lancamentos = await contexto.Lancamentos.ToListAsync();
            lancamentos.Should().ContainSingle()
                .Which.Situacao.Should().Be(SituacaoLancamento.InscritoEmDividaAtiva);

            var trilha = await contexto.AuditTrail.ToListAsync();
            trilha.Should().NotBeEmpty("o interceptor de auditoria deve registrar as alterações de estado");
        }

        // Assert — Tenant B NÃO enxerga nada do Tenant A (Global Query Filter).
        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.DividasAtivas.ToListAsync()).Should().BeEmpty();
            (await contexto.Contribuintes.ToListAsync()).Should().BeEmpty();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private TributosDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new TributosDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }
}

file sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserFake : ICurrentUser
{
    public string? UserId => "teste";

    public string? UserName => "Usuário de Teste";

    public string? IpAddress => "127.0.0.1";
}
