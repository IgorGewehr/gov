using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Cobertura de borda dos bugs do motor de depreciacao (MCASP / NBC TSP 07; escrutinio TCE):
/// BUG-P1 (recalculo da parcela apos reavaliacao/impairment sobre vida remanescente),
/// BUG-P2 (idempotencia por competencia), BUG-P3 (bem Cedido deprecia) e BUG-P4 (depreciacao de veiculo),
/// alem do fechamento de centavos ao longo da vida util (L-P2).
/// </summary>
public sealed class DepreciacaoBugTests : PatrimonioTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    private static BemPatrimonial BemTombadoEmUso(decimal inicial = 12000m, decimal residual = 2000m, int vida = 100)
    {
        var bem = BemPatrimonial.Incorporar(
            TenantA, "Notebook", TipoBem.Movel, ValorMonetario.De(inicial), ValorMonetario.De(residual),
            vida, new DateOnly(2026, 1, 10), "Aquisicao");
        bem.Tombar("TOMBO-DEP-BUG");
        bem.ColocarEmCondicoesDeUso();
        return bem;
    }

    private static Veiculo VeiculoTombadoEmUso(decimal inicial = 120000m, decimal residual = 20000m, int vida = 100)
    {
        var veiculo = Veiculo.IncorporarVeiculo(
            TenantA, "Caminhao", ValorMonetario.De(inicial), ValorMonetario.De(residual), vida,
            new DateOnly(2026, 1, 10), "Aquisicao",
            Placa.Criar("ABC1D23"), Renavam.Criar("12345678900"), Odometro.De(0), Horimetro.De(0m));
        veiculo.Tombar("TOMBO-VEIC-DEP");
        return veiculo;
    }

    // ---------- BUG-P1: recalculo apos reavaliacao/impairment ----------

    [Fact] // BUG-P1: apos reavaliacao, a parcela e (novoValor - residual) / vida remanescente.
    public void BugP1_parcela_recalcula_apos_reavaliacao()
    {
        // 12.000, residual 2.000, vida 100 -> 100/mes. Deprecia 50 meses -> contabil 7.000.
        var bem = BemTombadoEmUso(12000m, 2000m, 100);
        for (var mes = 1; mes <= 50; mes++)
        {
            bem.Depreciar(new DateOnly(2026, 1, 1).AddMonths(mes));
        }

        bem.ValorContabil.Valor.Should().Be(7000m);
        bem.VidaUtilRemanescenteMeses.Should().Be(50);

        // Reavalia para 15.000 (laudo). Novo depreciavel 13.000 / 50 meses remanescentes = 260/mes.
        bem.Reavaliar(15000m, "https://laudos/reav", Hoje);
        bem.ParcelaMensal.Should().Be(260m);

        var depreciado = bem.Depreciar(new DateOnly(2030, 4, 1));
        depreciado.Should().Be(260m);
        bem.ValorContabil.Valor.Should().Be(14740m);
    }

    [Fact] // BUG-P1: apos impairment, a parcela tambem recalcula sobre o novo valor e vida remanescente.
    public void BugP1_parcela_recalcula_apos_impairment()
    {
        var bem = BemTombadoEmUso(12000m, 2000m, 100);
        for (var mes = 1; mes <= 20; mes++)
        {
            bem.Depreciar(new DateOnly(2026, 1, 1).AddMonths(mes));
        }

        // Apos 20 meses: contabil 10.000, remanescente 80.
        bem.ValorContabil.Valor.Should().Be(10000m);
        bem.RegistrarImpairment(6000m, "https://laudos/imp", Hoje);

        // Novo depreciavel 6.000 - 2.000 = 4.000 / 80 = 50/mes.
        bem.ParcelaMensal.Should().Be(50m);
    }

    // ---------- L-P2: fechamento de centavos ----------

    [Fact] // L-P2: o somatorio das parcelas ao longo da vida = valor depreciavel exato (sem perder/criar centavo).
    public void LP2_somatorio_das_parcelas_fecha_no_valor_depreciavel()
    {
        // 1.000 / 3 meses, residual 0 -> 333,33 + 333,33 + resto.
        var bem = BemTombadoEmUso(1000m, 0m, 3);

        var total = 0m;
        for (var mes = 1; mes <= 3; mes++)
        {
            total += bem.Depreciar(new DateOnly(2026, 1, 1).AddMonths(mes));
        }

        total.Should().Be(1000m);
        bem.ValorContabil.Valor.Should().Be(0m);
    }

    // ---------- BUG-P2: idempotencia por competencia ----------

    [Fact] // BUG-P2: depreciar a MESMA competencia 2x e no-op — nao duplica despesa/historico/evento.
    public void BugP2_depreciar_mesma_competencia_e_idempotente()
    {
        var bem = BemTombadoEmUso(12000m, 2000m, 100);
        var competencia = new DateOnly(2026, 2, 1);

        var primeira = bem.Depreciar(competencia);
        var segunda = bem.Depreciar(competencia);

        primeira.Should().Be(100m);
        segunda.Should().Be(0m);
        bem.ValorContabil.Valor.Should().Be(11900m);
        bem.HistoricosDepreciacao.Should().ContainSingle();
        bem.DomainEvents.OfType<BemDepreciado>().Should().ContainSingle();
    }

    [Fact] // BUG-P2: competencias DIFERENTES continuam depreciando normalmente.
    public void BugP2_competencias_diferentes_depreciam()
    {
        var bem = BemTombadoEmUso(12000m, 2000m, 100);

        bem.Depreciar(new DateOnly(2026, 2, 1));
        bem.Depreciar(new DateOnly(2026, 3, 1));

        bem.HistoricosDepreciacao.Should().HaveCount(2);
        bem.ValorContabil.Valor.Should().Be(11800m);
    }

    // ---------- BUG-P3: bem Cedido deprecia ----------

    [Fact] // BUG-P3: bem Cedido permanece no acervo e deprecia (MCASP).
    public void BugP3_bem_cedido_deprecia()
    {
        var bem = BemTombadoEmUso(12000m, 2000m, 100);
        bem.Ceder();
        bem.Situacao.Should().Be(SituacaoBemPatrimonial.Cedido);

        var depreciado = bem.Depreciar(new DateOnly(2026, 2, 1));

        depreciado.Should().Be(100m);
        bem.ValorContabil.Valor.Should().Be(11900m);
        bem.HistoricosDepreciacao.Should().ContainSingle();
    }

    // ---------- BUG-P4: depreciacao de veiculo ----------

    [Fact] // BUG-P4: veiculo deprecia linearmente (e-um bem patrimonial).
    public void BugP4_veiculo_deprecia()
    {
        // 120.000, residual 20.000, vida 100 -> (120.000-20.000)/100 = 1.000/mes.
        var veiculo = VeiculoTombadoEmUso(120000m, 20000m, 100);

        var depreciado = veiculo.Depreciar(new DateOnly(2026, 2, 1));

        depreciado.Should().Be(1000m);
        veiculo.ValorContabil.Valor.Should().Be(119000m);
        veiculo.DomainEvents.OfType<VeiculoDepreciado>().Should().ContainSingle();
    }

    [Fact] // BUG-P4: a depreciacao do veiculo respeita o piso no residual.
    public void BugP4_veiculo_nao_deprecia_abaixo_do_residual()
    {
        // 21.000, residual 20.000, vida 1 -> parcela 1.000, restando exatamente o residual.
        var veiculo = VeiculoTombadoEmUso(21000m, 20000m, 1);

        veiculo.Depreciar(new DateOnly(2026, 2, 1));
        veiculo.ValorContabil.Valor.Should().Be(20000m);

        var segunda = veiculo.Depreciar(new DateOnly(2026, 3, 1));
        segunda.Should().Be(0m);
        veiculo.ValorContabil.Valor.Should().Be(20000m);
    }

    [Fact] // BUG-P4/BUG-P2: a depreciacao do veiculo e idempotente por competencia.
    public void BugP4_veiculo_idempotente_por_competencia()
    {
        var veiculo = VeiculoTombadoEmUso(120000m, 20000m, 100);
        var competencia = new DateOnly(2026, 2, 1);

        veiculo.Depreciar(competencia);
        var segunda = veiculo.Depreciar(competencia);

        segunda.Should().Be(0m);
        veiculo.HistoricosDepreciacao.Should().ContainSingle();
        veiculo.ValorContabil.Valor.Should().Be(119000m);
    }

    [Fact] // BUG-P4: veiculo nao ativo no acervo (apenas EmIncorporacao) nao deprecia.
    public void BugP4_veiculo_nao_ativo_no_acervo_nao_deprecia()
    {
        var veiculo = Veiculo.IncorporarVeiculo(
            TenantA, "Caminhao", ValorMonetario.De(120000m), ValorMonetario.De(20000m), 100,
            new DateOnly(2026, 1, 10), "Aquisicao",
            Placa.Criar("XYZ9Z88"), Renavam.Criar("12345678900"), Odometro.De(0), Horimetro.De(0m));

        ((Action)(() => veiculo.Depreciar(new DateOnly(2026, 2, 1)))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // BUG-P4: a depreciacao do veiculo persiste o historico (tabela owned) e reidrata.
    public async Task BugP4_depreciacao_de_veiculo_persiste_historico()
    {
        VeiculoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var veiculo = VeiculoTombadoEmUso(120000m, 20000m, 100);
            id = veiculo.Id;
            contexto.Veiculos.Add(veiculo);
            await contexto.SaveChangesAsync();

            veiculo.Depreciar(new DateOnly(2026, 2, 1));
            veiculo.Depreciar(new DateOnly(2026, 3, 1));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var veiculo = await contexto.Veiculos.SingleAsync(v => v.Id == id);
            veiculo.HistoricosDepreciacao.Should().HaveCount(2);
            veiculo.ValorContabil.Valor.Should().Be(118000m);
        }
    }

    // ---------- L-P8: depreciar sem condicoes de uso retorna 0 ----------

    [Fact] // L-P8: bem tombado mas SEM condicoes de uso retorna 0 e nao registra historico.
    public void LP8_sem_condicoes_de_uso_retorna_zero()
    {
        var bem = BemPatrimonial.Incorporar(
            TenantA, "Notebook", TipoBem.Movel, ValorMonetario.De(12000m), ValorMonetario.De(2000m),
            100, new DateOnly(2026, 1, 10), "Aquisicao");
        bem.Tombar("TOMBO-SEM-USO");

        bem.Depreciar(new DateOnly(2026, 2, 1)).Should().Be(0m);
        bem.HistoricosDepreciacao.Should().BeEmpty();
    }
}
