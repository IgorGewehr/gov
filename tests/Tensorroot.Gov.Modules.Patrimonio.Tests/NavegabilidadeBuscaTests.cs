using FluentAssertions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Cobertura da NAVEGABILIDADE (Onda 0): list/search paginada de Bens, Veiculos e Itens de
/// almoxarifado, com filtros e isolamento por tenant (Global Query Filter), sobre SQLite em memoria.
/// </summary>
public sealed class NavegabilidadeBuscaTests : PatrimonioTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    // ---------- Bens ----------

    [Fact]
    public async Task Bens_busca_por_descricao_e_paginada_e_tenant_scoped()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Bens.Add(BemMovel("Notebook Dell Latitude"));
            contexto.Bens.Add(BemMovel("Cadeira giratoria"));
            contexto.Bens.Add(BemMovel("Notebook Lenovo ThinkPad"));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            contexto.Bens.Add(BemMovel("Notebook do tenant B"));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new BemPatrimonialRepository(contexto);
            var (itens, total) = await repo.BuscarAsync("notebook", null, null, 1, 10, default);

            total.Should().Be(2, "o tenant A so enxerga os seus bens que casam 'notebook'");
            itens.Should().OnlyContain(bem => bem.Descricao.Contains("Notebook", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Bens_filtro_por_tipo_e_paginacao_respeitados()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            for (var i = 0; i < 5; i++)
            {
                contexto.Bens.Add(BemMovel($"Item movel {i}"));
            }

            contexto.Bens.Add(BemPatrimonial.Incorporar(
                TenantA, "Predio sede", TipoBem.Imovel, ValorMonetario.De(100000m), ValorMonetario.De(0m),
                600, Hoje, "Doacao", valorTerreno: 40000m));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new BemPatrimonialRepository(contexto);

            var imoveis = await repo.BuscarAsync(null, TipoBem.Imovel, null, 1, 10, default);
            imoveis.Total.Should().Be(1);

            var pagina1 = await repo.BuscarAsync(null, TipoBem.Movel, null, 1, 2, default);
            pagina1.Total.Should().Be(5);
            pagina1.Itens.Should().HaveCount(2, "tamanho de pagina aplicado");
        }
    }

    // ---------- Veiculos ----------

    [Fact]
    public async Task Veiculos_busca_por_placa_funciona()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Veiculos.Add(NovoVeiculo("Gol", "ABC1D23", "12345678900"));
            contexto.Veiculos.Add(NovoVeiculo("Strada", "XYZ4E56", "98765432103"));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new VeiculoRepository(contexto);
            var porPlaca = await repo.BuscarAsync("ABC1D23", null, 1, 10, default);
            porPlaca.Total.Should().Be(1);
            porPlaca.Itens.Single().Descricao.Should().Be("Gol");

            var porDescricao = await repo.BuscarAsync("strada", null, 1, 10, default);
            porDescricao.Total.Should().Be(1);
        }
    }

    // ---------- Itens de almoxarifado ----------

    [Fact]
    public async Task Itens_busca_por_codigo_ou_descricao_e_filtro_abc()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.ItensEstoque.Add(ItemEstoque.Cadastrar(
                TenantA, "ALM-001", "Resma de papel A4", "cx", MetodoCusteio.Peps, PontoPedido.De(5m), CurvaABC.A));
            contexto.ItensEstoque.Add(ItemEstoque.Cadastrar(
                TenantA, "ALM-002", "Caneta esferografica", "un", MetodoCusteio.Medio, PontoPedido.De(10m), CurvaABC.C));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new ItemEstoqueRepository(contexto);

            var porCodigo = await repo.BuscarAsync("ALM-001", null, null, 1, 10, default);
            porCodigo.Total.Should().Be(1);
            porCodigo.Itens.Single().Descricao.Should().Be("Resma de papel A4");

            var porDescricao = await repo.BuscarAsync("caneta", null, null, 1, 10, default);
            porDescricao.Total.Should().Be(1);

            var classeA = await repo.BuscarAsync(null, null, CurvaABC.A, 1, 10, default);
            classeA.Total.Should().Be(1);
            classeA.Itens.Single().Codigo.Should().Be("ALM-001");
        }
    }

    private static BemPatrimonial BemMovel(string descricao)
        => BemPatrimonial.Incorporar(
            TenantA, descricao, TipoBem.Movel, ValorMonetario.De(3000m), ValorMonetario.De(300m),
            60, Hoje, "Aquisicao");

    private static Veiculo NovoVeiculo(string descricao, string placa, string renavam)
        => Veiculo.IncorporarVeiculo(
            TenantA, descricao, ValorMonetario.De(80000m), ValorMonetario.De(8000m),
            120, Hoje, "Aquisicao", Placa.Criar(placa), Renavam.Criar(renavam),
            Odometro.De(0), Horimetro.De(0m));
}
