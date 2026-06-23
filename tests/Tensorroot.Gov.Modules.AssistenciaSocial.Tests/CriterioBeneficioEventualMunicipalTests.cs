using FluentAssertions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Parametros;
using Xunit;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Tests;

/// <summary>
/// A-0 — cobertura do criterio MUNICIPAL de beneficio eventual: prova que NAO ha teto federal de 1/4 SM
/// (revogado pela Lei 12.435/2011), que o corte de renda vem 100% da lei municipal (versionada por
/// tenant+vigencia) e que a ausencia de parametrizacao municipal NAO aplica default federal.
/// </summary>
public sealed class CriterioBeneficioEventualMunicipalTests : AssistenciaSocialTestBase
{
    private static readonly Competencia Junho2026 = Competencia.De(2026, 6);
    private static readonly ValorMonetario Sm = ValorMonetario.De(1412m);

    [Fact] // REGRESSAO do teto revogado: concede acima de 1/4 SM quando a lei municipal admite (1 SM).
    public void Concede_acima_de_um_quarto_sm_conforme_lei_municipal()
    {
        var criterio = CriterioBeneficioEventual.MunicipalVigente(ModalidadeBeneficioEventual.VulnerabilidadeTemporaria, multiploRendaSalarioMinimo: 1m);

        // 1/4 SM = 353; renda 900 > 353 mas <= 1412 (1 SM, lei municipal) => elegivel.
        criterio.Avaliar(RendaPerCapita.Calcular(900m, 1), Sm).Elegivel.Should().BeTrue();
    }

    [Fact] // Sem corte de renda na lei municipal (ex.: natalidade/morte) => concede independente da renda.
    public void Sem_corte_de_renda_concede_independente_da_renda()
    {
        var criterio = CriterioBeneficioEventual.MunicipalVigente(ModalidadeBeneficioEventual.Morte, multiploRendaSalarioMinimo: null);

        criterio.Avaliar(RendaPerCapita.Calcular(9999m, 1), Sm).Elegivel.Should().BeTrue();
    }

    [Fact] // Acima do corte municipal => indeferido.
    public void Acima_do_corte_municipal_indefere()
    {
        var criterio = CriterioBeneficioEventual.MunicipalVigente(ModalidadeBeneficioEventual.VulnerabilidadeTemporaria, multiploRendaSalarioMinimo: 0.5m);

        criterio.Avaliar(RendaPerCapita.Calcular(800m, 1), Sm).Elegivel.Should().BeFalse(); // 800 > 706
    }

    [Fact]
    public async Task Provider_le_o_criterio_municipal_versionado_e_isola_por_tenant()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            // Lei municipal: vulnerabilidade temporaria com corte de 1/2 SM a partir de 2024.
            await ctx.CriteriosBeneficioEventual.AddAsync(
                CriterioBeneficioEventualMunicipal.Criar(TenantA, ModalidadeBeneficioEventual.VulnerabilidadeTemporaria, new DateOnly(2024, 1, 1), 0.5m));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var provider = new CriterioBeneficioEventualProvider(ctx);

            var criterio = await provider.ObterCriterioVigenteAsync(ModalidadeBeneficioEventual.VulnerabilidadeTemporaria, Junho2026, CancellationToken.None);
            criterio.Should().NotBeNull();
            criterio!.MultiploRendaSalarioMinimo.Should().Be(0.5m);

            // A-0: modalidade nao parametrizada NAO retorna default federal — retorna nulo (sem teto 1/4 SM).
            var ausente = await provider.ObterCriterioVigenteAsync(ModalidadeBeneficioEventual.Natalidade, Junho2026, CancellationToken.None);
            ausente.Should().BeNull();
        }

        // Tenant B nao enxerga o criterio do tenant A (isolamento).
        await using (var ctx = CriarContexto(TenantB))
        {
            var provider = new CriterioBeneficioEventualProvider(ctx);
            var criterio = await provider.ObterCriterioVigenteAsync(ModalidadeBeneficioEventual.VulnerabilidadeTemporaria, Junho2026, CancellationToken.None);
            criterio.Should().BeNull();
        }
    }
}
