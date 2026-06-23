using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Tributos.Application.Cosip;
using Tensorroot.Gov.Modules.Tributos.Application.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Application.Taxas;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Cosip;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Domain.Taxas;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova de ponta-a-ponta da persistência de TAXAS, COSIP e CONTRIBUIÇÃO DE MELHORIA sobre SQLite em
/// memória, com auditoria e ISOLAMENTO por tenant (Global Query Filter). Exercita os handlers reais
/// (cálculo → Lancamento + DAM). Nenhum valor é hardcoded — tudo vem da configuração (lei municipal).
/// </summary>
public sealed class TaxasCosipMelhoriaPersistenciaTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("aaaa5555-5555-5555-5555-555555555555");
    private static readonly Guid TenantB = Guid.Parse("bbbb6666-6666-6666-6666-666666666666");

    private readonly SqliteConnection _connection;

    public TaxasCosipMelhoriaPersistenciaTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Lancar_taxa_persiste_lancamento_e_dam_e_isola_por_tenant()
    {
        ContribuinteId contribuinteId;

        await using (var contexto = CriarContexto(TenantA))
        {
            var contribuinte = Contribuinte.PessoaJuridica(TenantA, Cnpj.Create("11.222.333/0001-81"), "Comércio ME");
            contexto.Contribuintes.Add(contribuinte);
            contribuinteId = contribuinte.Id;

            var taxa = TabelaTaxa.Criar(TenantA, "TXFISC", "Taxa de fiscalização", EspecieTaxa.PoderPolicia, ModoCalculoTaxa.PorUnidade, 2026, ValorMonetario.De(1.50m), "CTM art. X");
            taxa.Publicar();
            contexto.TabelasTaxa.Add(taxa);
            await contexto.SaveChangesAsync();

            var handler = new LancarTaxaHandler(
                new TabelaTaxaRepository(contexto),
                new LancamentoRepository(contexto),
                new DamRepository(contexto),
                contexto,
                new TenantContextFakeTaxas(TenantA));

            var resultado = await handler.Handle(
                new LancarTaxaCommand(contribuinteId.Value, "TXFISC", 2026, QuantidadeBase: 200m, new DateOnly(2026, 7, 1)),
                CancellationToken.None);

            resultado.ValorTaxa.Should().Be(300m, "1,50/m² × 200 m²");
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var lancamento = await contexto.Lancamentos.SingleAsync();
            lancamento.TipoTributo.Should().Be(TipoTributo.Taxa);
            lancamento.ValorPrincipal.Valor.Should().Be(300m);
            var dam = await contexto.Dams.Include(d => d.Parcelas).SingleAsync();
            dam.ValorTotal.Valor.Should().Be(300m);
            (await contexto.AuditTrail.AnyAsync()).Should().BeTrue();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Lancamentos.AnyAsync()).Should().BeFalse();
            (await contexto.TabelasTaxa.AnyAsync()).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Lancar_cosip_propria_persiste_e_isola_por_tenant()
    {
        ContribuinteId contribuinteId;

        await using (var contexto = CriarContexto(TenantA))
        {
            var contribuinte = Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "Consumidor");
            contexto.Contribuintes.Add(contribuinte);
            contribuinteId = contribuinte.Id;

            var tabela = TabelaCosip.Criar(TenantA, 2026, "Lei COSIP nº X");
            tabela.DefinirFaixa(ClasseConsumidorCosip.Residencial, 0m, 100m, ValorMonetario.De(5m));
            tabela.DefinirFaixa(ClasseConsumidorCosip.Residencial, 100.01m, 300m, ValorMonetario.De(12m));
            tabela.Publicar();
            contexto.TabelasCosip.Add(tabela);
            await contexto.SaveChangesAsync();

            var handler = new LancarCosipHandler(
                new TabelaCosipRepository(contexto),
                new LancamentoRepository(contexto),
                new DamRepository(contexto),
                contexto,
                new TenantContextFakeTaxas(TenantA));

            var resultado = await handler.Handle(
                new LancarCosipCommand(contribuinteId.Value, ClasseConsumidorCosip.Residencial, ConsumoKwh: 250m, Ano: 2026, Mes: 5, new DateOnly(2026, 6, 10)),
                CancellationToken.None);

            resultado.ValorCosip.Should().Be(12m, "faixa 100,01–300 kWh");
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var lancamento = await contexto.Lancamentos.SingleAsync();
            lancamento.TipoTributo.Should().Be(TipoTributo.Cosip);
            lancamento.ValorPrincipal.Valor.Should().Be(12m);
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.TabelasCosip.AnyAsync()).Should().BeFalse();
            (await contexto.Lancamentos.AnyAsync()).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Ratear_melhoria_gera_um_lancamento_por_imovel_e_isola_por_tenant()
    {
        Guid obraId;
        var imovelA = Guid.NewGuid();
        var imovelB = Guid.NewGuid();

        await using (var contexto = CriarContexto(TenantA))
        {
            var propA = Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "Proprietário A");
            var propB = Contribuinte.PessoaFisica(TenantA, Cpf.Create("168.995.350-09"), "Proprietário B");
            contexto.Contribuintes.AddRange(propA, propB);

            // Custo 100.000, financiar 50% → limite total 50.000. Valorizações A=60k, B=40k.
            var obra = ObraContribuicaoMelhoria.PublicarEdital(
                TenantA, "Pavimentação Rua A", "Memorial.", ValorMonetario.De(100_000m), 50m, "Zona 1", 100m,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), "Lei da obra + CTM");
            obra.AdicionarImovelBeneficiado(new ImovelId(imovelA), propA.Id, ValorMonetario.De(60_000m));
            obra.AdicionarImovelBeneficiado(new ImovelId(imovelB), propB.Id, ValorMonetario.De(40_000m));
            obra.EncerrarPrazoImpugnacao(new DateOnly(2026, 2, 1));
            contexto.ObrasContribuicaoMelhoria.Add(obra);
            obraId = obra.Id.Value;
            await contexto.SaveChangesAsync();

            var handler = new RatearContribuicaoMelhoriaHandler(
                new ObraContribuicaoMelhoriaRepository(contexto),
                new LancamentoRepository(contexto),
                new DamRepository(contexto),
                contexto,
                new TenantContextFakeTaxas(TenantA));

            var resultado = await handler.Handle(
                new RatearContribuicaoMelhoriaCommand(obraId, new DateOnly(2026, 7, 1), NumeroParcelas: 1),
                CancellationToken.None);

            resultado.TotalRateado.Should().Be(50_000m);
            resultado.Lancamentos.Should().HaveCount(2);
            resultado.Lancamentos.Single(l => l.ImovelId == imovelA).ContribuicaoRateada.Should().Be(30_000m);
            resultado.Lancamentos.Single(l => l.ImovelId == imovelB).ContribuicaoRateada.Should().Be(20_000m);
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var lancamentos = await contexto.Lancamentos.Where(l => l.TipoTributo == TipoTributo.ContribuicaoMelhoria).ToListAsync();
            lancamentos.Should().HaveCount(2);
            lancamentos.Sum(l => l.ValorPrincipal.Valor).Should().Be(50_000m);
            (await contexto.Dams.CountAsync()).Should().Be(2);

            var obra = await contexto.ObrasContribuicaoMelhoria.Include(o => o.Imoveis).SingleAsync();
            obra.Estado.Should().Be(EstadoObraMelhoria.Rateada);
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.ObrasContribuicaoMelhoria.AnyAsync()).Should().BeFalse();
            (await contexto.Lancamentos.AnyAsync()).Should().BeFalse();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private TributosDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFakeTaxas(tenantId);
        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFakeTaxas(), TimeProvider.System))
            .Options;

        var contexto = new TributosDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }
}

file sealed class TenantContextFakeTaxas(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserFakeTaxas : ICurrentUser
{
    public string? UserId => "teste-taxas";

    public string? UserName => "Usuário Taxas";

    public string? IpAddress => "127.0.0.1";
}
