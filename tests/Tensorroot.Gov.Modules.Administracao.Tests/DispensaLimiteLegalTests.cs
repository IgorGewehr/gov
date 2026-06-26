using FluentAssertions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Prova do TETO LEGAL da dispensa eletrônica em razão do valor (Lei 14.133/2021, art. 75, I/II): o
/// agregado é fail-closed — recusa item que faça o total estimado ULTRAPASSAR o limite vigente. O limite
/// NÃO é hardcoded (CLAUDE.md §16): entra como parâmetro do tenant. Aqui ancoramos no valor OFICIAL do
/// Decreto 12.343/2024 (vigente em 2025) — inciso II (compras e outros serviços) = R$ 62.725,59 —
/// confirmando que o agregado respeita exatamente o limite parametrizado.
/// </summary>
public sealed class DispensaLimiteLegalTests
{
    private static readonly Guid Tenant = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // Decreto 12.343/2024, Anexo — art. 75, II da Lei 14.133/2021 (compras e outros serviços), vigente 2025.
    private const decimal LimiteIncisoII_Dec12343 = 62_725.59m;

    private static DispensaEletronica AbrirComLimiteDec12343() =>
        DispensaEletronica.Abrir(
            Tenant,
            "Aquisição de material de expediente",
            FundamentoDispensaValor.OutrosServicosECompras,
            CriterioJulgamentoDispensa.MenorPreco,
            ValorMonetario.De(LimiteIncisoII_Dec12343),
            "Dec. 12.343/2024");

    [Fact] // No limite exato (R$ 62.725,59): permitido — o teto é inclusivo (não "ultrapassa").
    public void Total_igual_ao_limite_e_permitido()
    {
        var dispensa = AbrirComLimiteDec12343();

        dispensa.AdicionarItem(null, "Resmas de papel A4", 1m, ValorMonetario.De(LimiteIncisoII_Dec12343));

        dispensa.ValorTotalEstimado.Valor.Should().Be(LimiteIncisoII_Dec12343);
        dispensa.Itens.Should().HaveCount(1);
    }

    [Fact] // Um centavo acima do limite Dec. 12.343 (R$ 62.725,60): recusado (ato nulo — exigiria licitação).
    public void Total_um_centavo_acima_do_limite_e_recusado()
    {
        var dispensa = AbrirComLimiteDec12343();

        var inclusao = () => dispensa.AdicionarItem(
            null, "Item acima do teto", 1m, ValorMonetario.De(LimiteIncisoII_Dec12343 + 0.01m));

        inclusao.Should().Throw<InvalidOperationException>()
            .WithMessage("*Dec. 12.343/2024*");
        dispensa.Itens.Should().BeEmpty(); // fail-closed: o item NÃO entra
    }

    [Fact] // O estouro é avaliado sobre o TOTAL acumulado, não item a item.
    public void Soma_de_itens_que_estoura_o_teto_e_recusada()
    {
        var dispensa = AbrirComLimiteDec12343();
        dispensa.AdicionarItem(null, "Lote 1", 1m, ValorMonetario.De(40_000m));

        var segundoLote = () => dispensa.AdicionarItem(null, "Lote 2", 1m, ValorMonetario.De(30_000m)); // 70k > 62.725,59

        segundoLote.Should().Throw<InvalidOperationException>();
        dispensa.ValorTotalEstimado.Valor.Should().Be(40_000m); // só o 1º lote permaneceu
    }
}
