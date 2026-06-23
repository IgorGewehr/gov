using FluentAssertions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Testes-chave de dominio dos agregados <see cref="Cardapio"/> e <see cref="DistribuicaoMerenda"/>
/// (Onda 3b — Merenda/PNAE): planejamento, publicacao (I-M3), calculo de consumo per capita x comensais e
/// a distribuicao que baixa generos (I-M5) emitindo <see cref="MerendaDistribuida"/>. Dominio puro.
/// </summary>
public sealed class MerendaDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly EscolaId Escola = new(Guid.Parse("77777777-7777-7777-7777-777777777777"));
    private static readonly DateOnly Semana = new(2026, 6, 22);

    private static Cardapio CardapioComItem()
    {
        var cardapio = Cardapio.Planejar(Tenant, Escola, FaixaEtariaPnae.EnsinoFundamentalMedio, Semana);
        cardapio.AdicionarItem(DiaSemanaCardapio.Segunda, TipoRefeicao.Almoco, Guid.NewGuid(), 0.12m, "kg");
        return cardapio;
    }

    [Fact]
    public void Planejar_cardapio_deve_nascer_planejado_e_emitir_evento()
    {
        var cardapio = Cardapio.Planejar(Tenant, Escola, FaixaEtariaPnae.Creche, Semana);

        cardapio.Situacao.Should().Be(SituacaoCardapio.Planejado);
        cardapio.TenantId.Should().Be(Tenant);
        cardapio.DomainEvents.Should().ContainSingle(e => e is CardapioPlanejado);
    }

    [Fact]
    public void Publicar_cardapio_sem_itens_deve_falhar()
    {
        var cardapio = Cardapio.Planejar(Tenant, Escola, FaixaEtariaPnae.Creche, Semana);

        var act = () => cardapio.Publicar();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Publicar_cardapio_com_itens_deve_transitar_e_emitir_evento()
    {
        var cardapio = CardapioComItem();

        cardapio.Publicar();

        cardapio.Situacao.Should().Be(SituacaoCardapio.Publicado);
        cardapio.DomainEvents.Should().Contain(e => e is CardapioPublicado);
    }

    [Fact]
    public void Adicionar_item_em_cardapio_publicado_deve_falhar()
    {
        var cardapio = CardapioComItem();
        cardapio.Publicar();

        var act = () => cardapio.AdicionarItem(DiaSemanaCardapio.Terca, TipoRefeicao.Almoco, Guid.NewGuid(), 0.1m, "kg");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Calcular_consumo_deve_multiplicar_per_capita_por_comensais()
    {
        var cardapio = CardapioComItem();

        var consumo = cardapio.CalcularConsumo(DiaSemanaCardapio.Segunda, TipoRefeicao.Almoco, 100);

        consumo.Should().ContainSingle();
        consumo[0].Quantidade.Should().Be(12m);
        consumo[0].UnidadeMedida.Should().Be("kg");
    }

    [Fact]
    public void Registrar_distribuicao_deve_registrar_consumo_e_emitir_evento()
    {
        var cardapio = CardapioComItem();
        cardapio.Publicar();
        var previsto = cardapio.CalcularConsumo(DiaSemanaCardapio.Segunda, TipoRefeicao.Almoco, 100);

        var distribuicao = DistribuicaoMerenda.Registrar(
            Tenant, Escola, cardapio.Id, new DateOnly(2026, 6, 22), TipoRefeicao.Almoco, 100, previsto);

        distribuicao.Comensais.Should().Be(100);
        distribuicao.Consumos.Should().ContainSingle();
        distribuicao.DomainEvents.Should().Contain(e => e is MerendaDistribuida);
    }

    [Fact]
    public void Registrar_distribuicao_sem_genero_deve_falhar()
    {
        var act = () => DistribuicaoMerenda.Registrar(
            Tenant, Escola, CardapioId.New(), new DateOnly(2026, 6, 22), TipoRefeicao.Almoco, 100, []);

        act.Should().Throw<InvalidOperationException>();
    }
}
