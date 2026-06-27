using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Prova da consulta de retenções/consignações do extrato do credor (SW-A5): consolida via
/// Liquidacao→Empenho→Credor.Documento, agrupa por natureza e calcula o saldo a recolher
/// (retido − recolhido). Roda sobre SQLite com os Global Query Filters de tenant ativos.
/// </summary>
public sealed class CredorRetencoesExtratoTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private const string CnpjCredor = "11.222.333/0001-81";
    private const string DocumentoCredor = "11222333000181"; // normalizado (sem máscara)

    private readonly SqliteConnection _connection;

    public CredorRetencoesExtratoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Retencoes_do_credor_agrupam_por_natureza_e_calculam_saldo_a_reter()
    {
        var credor = Credor.PessoaJuridica("Fornecedor Exemplo LTDA", Cnpj.Create(CnpjCredor));

        await using (var ctx = CriarContexto(TenantA))
        {
            var empenho = Empenho.Emitir(
                TenantA, "2026NE000123", DotacaoOrcamentariaId.New(), credor,
                ValorMonetario.De(10_000m), TipoEmpenho.Ordinario, 2026, new DateOnly(2026, 6, 1));
            ctx.Empenhos.Add(empenho);

            var liquidacao = Liquidacao.Registrar(
                TenantA, empenho.Id, ValorMonetario.De(10_000m), new DateOnly(2026, 6, 10),
                DocumentoComprobatorio.NotaFiscal("NF-1", new DateOnly(2026, 6, 5)));

            // IRRF-PJ: R$ 120 retido + R$ 80 retido; o de R$ 80 já foi recolhido a terceiro por guia.
            var irrfA = Retencao.Criar(NaturezaRetencao.IrrfPessoaJuridica, ValorMonetario.De(120m), ValorMonetario.De(10_000m), "IRRF-PJ parcela A");
            var irrfB = Retencao.Criar(NaturezaRetencao.IrrfPessoaJuridica, ValorMonetario.De(80m), ValorMonetario.De(10_000m), "IRRF-PJ parcela B");
            irrfB.MarcarRecolhida(Guid.NewGuid());
            // ISS retido R$ 300, ainda em aberto.
            var iss = Retencao.Criar(NaturezaRetencao.IssRetido, ValorMonetario.De(300m), ValorMonetario.De(10_000m), "ISS retido");

            liquidacao.AdicionarRetencao(irrfA);
            liquidacao.AdicionarRetencao(irrfB);
            liquidacao.AdicionarRetencao(iss);
            ctx.Liquidacoes.Add(liquidacao);

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var consulta = new CredorExtratoConsulta(ctx);

            var retencoes = await consulta.ListarRetencoesDoCredorAsync(DocumentoCredor, exercicio: 2026, CancellationToken.None);

            retencoes.Should().HaveCount(2, "duas naturezas distintas: IRRF-PJ e ISS retido");

            var irrf = retencoes.Single(r => r.Natureza == NaturezaRetencao.IrrfPessoaJuridica.ToString());
            irrf.ValorRetido.Should().Be(200m, "120 + 80");
            irrf.ValorRecolhido.Should().Be(80m, "só a parcela B foi recolhida");
            irrf.ValorAReter.Should().Be(120m, "saldo a recolher = retido − recolhido");

            var issResumo = retencoes.Single(r => r.Natureza == NaturezaRetencao.IssRetido.ToString());
            issResumo.ValorRetido.Should().Be(300m);
            issResumo.ValorRecolhido.Should().Be(0m);
            issResumo.ValorAReter.Should().Be(300m);
        }
    }

    [Fact]
    public async Task Filtro_de_exercicio_exclui_empenhos_de_outro_ano()
    {
        var credor = Credor.PessoaJuridica("Fornecedor Exemplo LTDA", Cnpj.Create(CnpjCredor));

        await using (var ctx = CriarContexto(TenantA))
        {
            var empenho2025 = Empenho.Emitir(
                TenantA, "2025NE000001", DotacaoOrcamentariaId.New(), credor,
                ValorMonetario.De(5_000m), TipoEmpenho.Ordinario, 2025, new DateOnly(2025, 6, 1));
            ctx.Empenhos.Add(empenho2025);

            var liq = Liquidacao.Registrar(
                TenantA, empenho2025.Id, ValorMonetario.De(5_000m), new DateOnly(2025, 6, 10),
                DocumentoComprobatorio.NotaFiscal("NF-2025", new DateOnly(2025, 6, 5)));
            liq.AdicionarRetencao(Retencao.Criar(NaturezaRetencao.IrrfPessoaJuridica, ValorMonetario.De(75m), ValorMonetario.De(5_000m), "IRRF 2025"));
            ctx.Liquidacoes.Add(liq);

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var consulta = new CredorExtratoConsulta(ctx);

            (await consulta.ListarRetencoesDoCredorAsync(DocumentoCredor, exercicio: 2026, CancellationToken.None))
                .Should().BeEmpty("o empenho é de 2025, fora do exercício filtrado");

            (await consulta.ListarRetencoesDoCredorAsync(DocumentoCredor, exercicio: null, CancellationToken.None))
                .Should().ContainSingle("sem filtro de exercício, a retenção de 2025 aparece");
        }
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private FinancasDbContext CriarContexto(Guid tenantId)
    {
        var tenant = new TenantContextRetencoesFake(tenantId);
        var options = new DbContextOptionsBuilder<FinancasDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenant),
                new AuditSaveChangesInterceptor(new CurrentUserRetencoesFake(), TimeProvider.System))
            .Options;

        var contexto = new FinancasDbContext(options, tenant);
        contexto.Database.EnsureCreated();
        return contexto;
    }
}

file sealed class TenantContextRetencoesFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserRetencoesFake : ICurrentUser
{
    public string? UserId => "teste";

    public string? UserName => "Usuário de Teste";

    public string? IpAddress => "127.0.0.1";
}
