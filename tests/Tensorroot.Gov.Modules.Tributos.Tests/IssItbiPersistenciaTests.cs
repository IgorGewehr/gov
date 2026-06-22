using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova de ponta-a-ponta da persistência do ISS (apuração/livro) e do ITBI (transmissão) sobre SQLite
/// em memória, com auditoria e ISOLAMENTO por tenant (Global Query Filter).
/// </summary>
public sealed class IssItbiPersistenciaTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("aaaa3333-3333-3333-3333-333333333333");
    private static readonly Guid TenantB = Guid.Parse("bbbb4444-4444-4444-4444-444444444444");

    private readonly SqliteConnection _connection;

    public IssItbiPersistenciaTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Apuracao_iss_persiste_livro_e_isola_por_tenant()
    {
        ContribuinteId prestadorId;

        await using (var contexto = CriarContexto(TenantA))
        {
            var prestador = Contribuinte.PessoaJuridica(TenantA, Cnpj.Create("11.222.333/0001-81"), "Prestadora ME");
            contexto.Contribuintes.Add(prestador);
            prestadorId = prestador.Id;

            var tabela = TabelaAliquotaIss.Criar(TenantA, 202601, "CTM Municipal");
            tabela.DefinirItem("1.07", 2.0m);
            tabela.DefinirItem("7.02", 3.0m, retencaoObrigatoria: true);
            tabela.Publicar();
            contexto.TabelasAliquotaIss.Add(tabela);

            var competencia = Competencia.De(2026, 5);
            var apuracao = ApuracaoIss.Abrir(TenantA, prestadorId, competencia);

            var nota1 = NotaFiscalServico.Importar(TenantA, "CHV-A", "11222333000181", null, ValorMonetario.De(1_000m), ValorMonetario.De(0m), new DateOnly(2026, 5, 5), competencia, "1.07");
            var nota2 = NotaFiscalServico.Importar(TenantA, "CHV-B", "11222333000181", null, ValorMonetario.De(2_000m), ValorMonetario.De(0m), new DateOnly(2026, 5, 6), competencia, "7.02");
            apuracao.Escriturar(CalculadoraIss.Apurar(nota1, tabela));
            apuracao.Escriturar(CalculadoraIss.Apurar(nota2, tabela));
            apuracao.Encerrar();
            contexto.ApuracoesIss.Add(apuracao);

            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var apuracao = await contexto.ApuracoesIss.Include(a => a.Itens).SingleAsync();
            apuracao.IssProprio.Valor.Should().Be(20m, "item 1.07 R$1.000 @ 2%");
            apuracao.IssRetido.Valor.Should().Be(60m, "item 7.02 R$2.000 @ 3% com retenção obrigatória");
            apuracao.Itens.Should().HaveCount(2);
            (await contexto.AuditTrail.AnyAsync()).Should().BeTrue();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.ApuracoesIss.AnyAsync()).Should().BeFalse();
            (await contexto.TabelasAliquotaIss.AnyAsync()).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Transmissao_itbi_persiste_com_base_maior_valor_e_isola_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            var transmitente = Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "Vendedor");
            var adquirente = Contribuinte.PessoaFisica(TenantA, Cpf.Create("168.995.350-09"), "Comprador");
            contexto.Contribuintes.AddRange(transmitente, adquirente);

            var imovel = Imovel.Cadastrar(
                TenantA,
                transmitente.Id,
                IdentificacaoImovel.Criar("000.555.0001"),
                EnderecoImovel.Criar("Rua X", "Centro", "01-01-001", "ZONA-1"),
                CaracteristicasImovel.Criar(300m, 100m, TipoUsoImovel.Residencial, "MEDIO"));
            contexto.Imoveis.Add(imovel);

            var aliquota = AliquotaItbi.Criar(TenantA, 2026, 2.0m, 0.5m, "Lei ITBI Municipal");
            aliquota.Publicar();
            contexto.AliquotasItbi.Add(aliquota);

            // Venal R$ 200.000; declarado R$ 250.000 → base 250.000 × 2% = R$ 5.000.
            var memoria = CalculadoraItbi.Calcular(ValorMonetario.De(200_000m), ValorMonetario.De(250_000m), 2.0m, 0.5m);
            var transmissao = TransmissaoImobiliaria.Registrar(TenantA, imovel.Id, transmitente.Id, adquirente.Id, 2026, ValorMonetario.De(250_000m), memoria);
            contexto.TransmissoesImobiliarias.Add(transmissao);

            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var transmissao = await contexto.TransmissoesImobiliarias.SingleAsync();
            transmissao.BaseCalculo.Valor.Should().Be(250_000m);
            transmissao.BaseFoiValorVenal.Should().BeFalse();
            transmissao.ImpostoDevido.Valor.Should().Be(5_000m);
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.TransmissoesImobiliarias.AnyAsync()).Should().BeFalse();
            (await contexto.AliquotasItbi.AnyAsync()).Should().BeFalse();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private TributosDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFakeIssItbi(tenantId);
        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFakeIssItbi(), TimeProvider.System))
            .Options;

        var contexto = new TributosDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }
}

file sealed class TenantContextFakeIssItbi(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserFakeIssItbi : ICurrentUser
{
    public string? UserId => "teste-iss-itbi";

    public string? UserName => "Usuário ISS/ITBI";

    public string? IpAddress => "127.0.0.1";
}
