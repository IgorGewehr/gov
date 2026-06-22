using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Prova do módulo Finanças sobre SQLite: ciclo Empenho → Liquidação → Pagamento (Lei 4.320),
/// gravação de auditoria e isolamento entre tenants.
/// </summary>
public sealed class EmpenhoFluxoTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly SqliteConnection _connection;

    public EmpenhoFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Ciclo_empenho_liquidacao_pagamento_persiste_e_respeita_isolamento()
    {
        var credor = Credor.PessoaJuridica("Fornecedor Exemplo LTDA", Cnpj.Create("11.222.333/0001-81"));

        await using (var contexto = CriarContexto(TenantA))
        {
            var empenho = Empenho.Emitir(
                TenantA,
                "2026NE000123",
                DotacaoOrcamentariaId.New(),
                credor,
                ValorMonetario.De(10000.00m),
                TipoEmpenho.Ordinario,
                2026,
                new DateOnly(2026, 6, 1));
            empenho.RegistrarLiquidacao(ValorMonetario.De(10000.00m));
            empenho.RegistrarPagamento(ValorMonetario.De(10000.00m));
            contexto.Empenhos.Add(empenho);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var empenho = await contexto.Empenhos.FirstAsync();
            empenho.Situacao.Should().Be(SituacaoEmpenho.TotalmentePago);
            empenho.SaldoALiquidar.Valor.Should().Be(0m);
            empenho.SaldoAPagar.Valor.Should().Be(0m);
            (await contexto.AuditTrail.ToListAsync()).Should().NotBeEmpty();
        }

        await using (var contexto = CriarContexto(Guid.NewGuid()))
        {
            (await contexto.Empenhos.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact]
    public void Pagamento_sem_liquidacao_previa_e_bloqueado()
    {
        var credor = Credor.PessoaJuridica("Fornecedor Exemplo LTDA", Cnpj.Create("11.222.333/0001-81"));
        var empenho = Empenho.Emitir(
            TenantA,
            "2026NE000999",
            DotacaoOrcamentariaId.New(),
            credor,
            ValorMonetario.De(500m),
            TipoEmpenho.Ordinario,
            2026,
            new DateOnly(2026, 6, 1));

        var acao = () => empenho.RegistrarPagamento(ValorMonetario.De(500m));

        acao.Should().Throw<Domain.Exceptions.SaldoLiquidacaoInsuficienteException>();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private FinancasDbContext CriarContexto(Guid tenantId)
    {
        var tenant = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<FinancasDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenant),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new FinancasDbContext(options, tenant);
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
