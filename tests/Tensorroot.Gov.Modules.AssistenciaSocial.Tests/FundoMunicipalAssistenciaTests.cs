using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Xunit;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Tests;

/// <summary>
/// A-1 — cobertura do Fundo Municipal de Assistencia Social (FMAS) por bloco/piso de cofinanciamento
/// SUAS (Port. 1.043/2024), sobre SQLite em memoria com isolamento por tenant: execucao segregada por
/// bloco e por piso, transposicao vedada, persistencia das contas e isolamento entre tenants. Espelha o
/// padrao do FMS da Saude (FundoMunicipalSaudeTests).
/// </summary>
public sealed class FundoMunicipalAssistenciaTests : AssistenciaSocialTestBase
{
    [Fact]
    public void FMAS_segrega_a_execucao_por_bloco_e_por_piso()
    {
        var fundo = FundoMunicipalAssistencia.Criar(TenantA, "FMAS Teste", "12345678000199");

        // PSB/Piso Basico Fixo (PAIF) e PSE-MC/Piso Fixo MC (PAEFI) — blocos da Port. 1.043/2024.
        fundo.ReceberParcela(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica, PisoAssistencia.BasicoFixo, "fonte-psb", 100_000m);
        fundo.ReceberParcela(BlocoFinanciamentoAssistencia.ProtecaoSocialEspecialMediaComplexidade, PisoAssistencia.FixoMediaComplexidade, "fonte-pse", 60_000m);
        fundo.ExecutarDespesa(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica, PisoAssistencia.BasicoFixo, "fonte-psb", 40_000m);

        fundo.RecebidoDoBloco(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica).Should().Be(100_000m);
        fundo.ExecutadoDoBloco(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica).Should().Be(40_000m);
        fundo.SaldoDoBloco(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica).Should().Be(60_000m);
        fundo.SaldoDoPiso(PisoAssistencia.BasicoFixo).Should().Be(60_000m);

        // O bloco PSE-MC e independente — execucao da PSB nao o toca.
        fundo.SaldoDoBloco(BlocoFinanciamentoAssistencia.ProtecaoSocialEspecialMediaComplexidade).Should().Be(60_000m);
        fundo.ExecutadoDoBloco(BlocoFinanciamentoAssistencia.ProtecaoSocialEspecialMediaComplexidade).Should().Be(0m);
        fundo.SaldoDoPiso(PisoAssistencia.FixoMediaComplexidade).Should().Be(60_000m);
    }

    [Fact]
    public void FMAS_veda_transposicao_entre_blocos_e_pisos()
    {
        var fundo = FundoMunicipalAssistencia.Criar(TenantA, "FMAS Teste", "12345678000199");
        fundo.ReceberParcela(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica, PisoAssistencia.BasicoFixo, "fonte-psb", 50_000m);

        // Executar em outro bloco/piso sem saldo nele (mesmo havendo saldo no Basico Fixo) -> bloqueado.
        var transpor = () => fundo.ExecutarDespesa(
            BlocoFinanciamentoAssistencia.ProtecaoSocialEspecialAltaComplexidade, PisoAssistencia.Acolhimento, "fonte-pse-ac", 10_000m);
        transpor.Should().Throw<InvalidOperationException>().WithMessage("*vedada*");

        // Executar alem do saldo da propria conta tambem e bloqueado.
        var excede = () => fundo.ExecutarDespesa(
            BlocoFinanciamentoAssistencia.ProtecaoSocialBasica, PisoAssistencia.BasicoFixo, "fonte-psb", 60_000m);
        excede.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FMAS_cobre_os_quatro_blocos_de_cofinanciamento()
    {
        var fundo = FundoMunicipalAssistencia.Criar(TenantA, "FMAS Teste", "12345678000199");

        fundo.ReceberParcela(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica, PisoAssistencia.BasicoFixo, "f1", 10m);
        fundo.ReceberParcela(BlocoFinanciamentoAssistencia.ProtecaoSocialEspecialMediaComplexidade, PisoAssistencia.FixoMediaComplexidade, "f2", 20m);
        fundo.ReceberParcela(BlocoFinanciamentoAssistencia.ProtecaoSocialEspecialAltaComplexidade, PisoAssistencia.Acolhimento, "f3", 30m);
        fundo.ReceberParcela(BlocoFinanciamentoAssistencia.GestaoSuasIgd, PisoAssistencia.Gestao, "f4", 40m);

        // Os 4 blocos da Port. 1.043/2024 ficam segregados.
        fundo.RecebidoDoBloco(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica).Should().Be(10m);
        fundo.RecebidoDoBloco(BlocoFinanciamentoAssistencia.ProtecaoSocialEspecialMediaComplexidade).Should().Be(20m);
        fundo.RecebidoDoBloco(BlocoFinanciamentoAssistencia.ProtecaoSocialEspecialAltaComplexidade).Should().Be(30m);
        fundo.RecebidoDoBloco(BlocoFinanciamentoAssistencia.GestaoSuasIgd).Should().Be(40m);
    }

    [Fact]
    public async Task FMAS_persiste_e_recarrega_contas_por_bloco_piso()
    {
        FundoMunicipalAssistenciaId fundoId;
        await using (var ctx = CriarContexto(TenantA))
        {
            var fundo = FundoMunicipalAssistencia.Criar(TenantA, "FMAS Maximiliano", "12345678000199");
            await ctx.FundosMunicipaisAssistencia.AddAsync(fundo);
            fundo.ReceberParcela(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica, PisoAssistencia.BasicoFixo, "fonte-psb", 90_000m);
            fundo.ReceberParcela(BlocoFinanciamentoAssistencia.GestaoSuasIgd, PisoAssistencia.Gestao, "fonte-igd", 30_000m);
            await ctx.SaveChangesAsync();
            fundoId = fundo.Id;
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var recarregado = await ctx.FundosMunicipaisAssistencia.FirstAsync(f => f.Id == fundoId);
            recarregado.Contas.Should().HaveCount(2);
            recarregado.RecebidoDoBloco(BlocoFinanciamentoAssistencia.ProtecaoSocialBasica).Should().Be(90_000m);
            recarregado.RecebidoDoPiso(PisoAssistencia.Gestao).Should().Be(30_000m);
        }
    }

    [Fact]
    public async Task FMAS_isola_por_tenant()
    {
        FundoMunicipalAssistenciaId idDoA;
        await using (var ctx = CriarContexto(TenantA))
        {
            var fundo = FundoMunicipalAssistencia.Criar(TenantA, "FMAS A", "12345678000199");
            await ctx.FundosMunicipaisAssistencia.AddAsync(fundo);
            await ctx.SaveChangesAsync();
            idDoA = fundo.Id;
        }

        // O tenant B nao enxerga o fundo do tenant A (Global Query Filter).
        await using (var ctx = CriarContexto(TenantB))
        {
            (await ctx.FundosMunicipaisAssistencia.AnyAsync(f => f.Id == idDoA)).Should().BeFalse();
            (await ctx.FundosMunicipaisAssistencia.CountAsync()).Should().Be(0);
        }
    }
}
