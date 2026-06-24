using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova de ponta-a-ponta da persistência do IPTU sobre SQLite em memória: cadastro imobiliário,
/// PGV/alíquotas parametrizadas, lançamento anual com DAM/parcelas, auditoria e ISOLAMENTO por tenant.
/// </summary>
public sealed class ImovelIptuPersistenciaTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = Guid.Parse("bbbb2222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;

    public ImovelIptuPersistenciaTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Cadastro_pgv_e_lancamento_anual_persistem_com_dam_auditoria_e_isolamento()
    {
        ContribuinteId proprietarioId;
        ImovelId imovelId;

        // Arrange + Act — Tenant A: contribuinte, imóvel, PGV, alíquotas; apura e lança o IPTU com DAM em 2 parcelas.
        await using (var contexto = CriarContexto(TenantA))
        {
            var contribuinte = Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "João Proprietário");
            contexto.Contribuintes.Add(contribuinte);
            proprietarioId = contribuinte.Id;

            var imovel = Imovel.Cadastrar(
                TenantA,
                proprietarioId,
                IdentificacaoImovel.Criar("000.123.4567", cibCodigo: "ABC1234-5"),
                EnderecoImovel.Criar("Rua Central", "Centro", "01-02-003", "ZONA-1", numero: "100"),
                CaracteristicasImovel.Criar(360m, 150m, TipoUsoImovel.Residencial, "MEDIO"));
            contexto.Imoveis.Add(imovel);
            imovelId = imovel.Id;

            var pgv = PlantaValores.Criar(TenantA, 2026, "Lei PGV Municipal");
            pgv.DefinirZona("ZONA-1", valorM2Terreno: 250m, valorM2Construcao: 1200m);
            pgv.Publicar();
            contexto.PlantasValores.Add(pgv);

            var tabela = TabelaAliquotaIptu.Criar(TenantA, 2026, edificado: true, "Lei Alíquotas Municipal");
            tabela.AdicionarFaixa(0m, TabelaAliquotaIptu.SemTeto, 1.0m);
            tabela.Publicar();
            contexto.TabelasAliquotaIptu.Add(tabela);

            await contexto.SaveChangesAsync();

            // Valor venal = 360×250 (90.000) + 150×1200 (180.000) = 270.000; IPTU 1% = 2.700.
            var memoria = CalculadoraIptu.Calcular(imovel, pgv, tabela);
            memoria.ImpostoDevido.Valor.Should().Be(2_700m);

            var lancamento = Lancamento.LancarIptu(TenantA, proprietarioId, imovelId, 2026, memoria.ImpostoDevido, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 10));
            contexto.Lancamentos.Add(lancamento);

            var dam = Dam.Gerar(TenantA, lancamento.Id, proprietarioId, memoria.ImpostoDevido, numeroParcelas: 2, new DateOnly(2026, 3, 10));
            contexto.Dams.Add(dam);
            await contexto.SaveChangesAsync();
        }

        // Assert — Tenant A relê o estado e a trilha de auditoria.
        await using (var contexto = CriarContexto(TenantA))
        {
            var imovel = await contexto.Imoveis.SingleAsync();
            imovel.Identificacao.InscricaoMunicipal.Should().Be("000.123.4567");
            imovel.Identificacao.CibCodigo.Should().Be("ABC1234-5");
            imovel.Endereco.ZonaFiscal.Should().Be("ZONA-1");
            imovel.Caracteristicas.AreaConstruida.Should().Be(150m);

            var lancamento = await contexto.Lancamentos.SingleAsync(l => l.TipoTributo == TipoTributo.Iptu);
            lancamento.ImovelId.Should().Be(imovelId);
            lancamento.ValorPrincipal.Valor.Should().Be(2_700m);

            var dam = await contexto.Dams.Include(d => d.Parcelas).SingleAsync();
            dam.Parcelas.Should().HaveCount(2);
            dam.Parcelas.Sum(p => p.Valor.Valor).Should().Be(2_700m, "a soma das parcelas deve fechar com o total");

            (await contexto.AuditTrail.AnyAsync()).Should().BeTrue("o interceptor de auditoria deve registrar as mutações");
        }

        // Assert — Tenant B NÃO enxerga nada do Tenant A (Global Query Filter).
        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Imoveis.AnyAsync()).Should().BeFalse();
            (await contexto.PlantasValores.AnyAsync()).Should().BeFalse();
            (await contexto.TabelasAliquotaIptu.AnyAsync()).Should().BeFalse();
            (await contexto.Dams.AnyAsync()).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Pgv_parametrizada_persiste_zonas_e_fatores_e_e_relida_pelo_motor()
    {
        Guid imovelId;
        await using (var contexto = CriarContexto(TenantA))
        {
            var contribuinte = Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "Ana");
            contexto.Contribuintes.Add(contribuinte);

            var imovel = Imovel.Cadastrar(
                TenantA,
                contribuinte.Id,
                IdentificacaoImovel.Criar("999.000.1111"),
                EnderecoImovel.Criar("Av. Brasil", "Industrial", "09-09-009", "ZONA-IND"),
                CaracteristicasImovel.Criar(1000m, 400m, TipoUsoImovel.Industrial, "ALTO"));
            contexto.Imoveis.Add(imovel);
            imovelId = imovel.Id.Value;

            var pgv = PlantaValores.Criar(TenantA, 2026, "Lei PGV");
            pgv.DefinirZona("ZONA-IND", 80m, 600m);
            pgv.DefinirFator(TipoFatorPgv.PadraoConstrutivo, "ALTO", 1.5m);
            pgv.Publicar();
            contexto.PlantasValores.Add(pgv);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var imovel = await contexto.Imoveis.SingleAsync(i => i.Id == new ImovelId(imovelId));
            var pgv = await contexto.PlantasValores
                .Include(p => p.Zonas)
                .Include(p => p.Fatores)
                .SingleAsync();

            pgv.Zonas.Should().ContainSingle();
            pgv.Fatores.Should().ContainSingle(f => f.Tipo == TipoFatorPgv.PadraoConstrutivo && f.Chave == "ALTO");

            // Valor venal = 1000×80 (80.000) + 400×600×1.5 (360.000) = 440.000 — fator relido da PGV.
            var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);
            memoria.FatorPadrao.Should().Be(1.5m);
            memoria.ValorVenal.Valor.Should().Be(440_000m);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private TributosDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFakeIptu(tenantId);
        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFakeIptu(), TimeProvider.System))
            .Options;

        var contexto = new TributosDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }
}

file sealed class TenantContextFakeIptu(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserFakeIptu : ICurrentUser
{
    public string? UserId => "teste-iptu";

    public string? UserName => "Usuário IPTU";

    public string? IpAddress => "127.0.0.1";
}
