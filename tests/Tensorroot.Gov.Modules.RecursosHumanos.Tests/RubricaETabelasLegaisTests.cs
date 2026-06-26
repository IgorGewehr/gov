using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura das rubricas parametrizaveis (invariantes de incidencia/natureza) e das tabelas legais
/// versionadas: persistencia das faixas owned, unicidade por (tenant, vigencia) e ISOLAMENTO por
/// tenant (Global Query Filter) — uma prefeitura nunca enxerga rubricas/tabelas de outra.
/// </summary>
public sealed class RubricaETabelasLegaisTests : RecursosHumanosTestBase
{
    // ---------- Rubrica (dominio) ----------

    [Fact] // Rubrica informativa nao pode ter incidencia previdenciaria/IRRF (verificacao-esocial §3.1).
    public void Rubrica_informativa_nao_admite_incidencia()
    {
        var acao = () => RubricaFolha.Criar(
            TenantA, Rubrica.De("INFO"), "Base informativa", NaturezaRubrica.Informativa, Competencia.De(2026, 1),
            incideInss: true);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // Rubrica nao pode ter valor fixo e percentual simultaneamente.
    public void Rubrica_nao_admite_valor_e_percentual_juntos()
    {
        var acao = () => RubricaFolha.Criar(
            TenantA, Rubrica.De("X"), "X", NaturezaRubrica.Provento, Competencia.De(2026, 1),
            valorFixo: 100m, percentual: 0.1m);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // Provento com incidencias nasce valido e expoe as flags.
    public void Rubrica_provento_com_incidencias_nasce_valida()
    {
        var rubrica = RubricaFolha.Criar(
            TenantA, Rubrica.De("VENCIMENTO"), "Vencimento base", NaturezaRubrica.Provento, Competencia.De(2026, 1),
            incideInss: true, incideRpps: true, incideIrrf: true, incideFgts: true);

        rubrica.EhProvento.Should().BeTrue();
        rubrica.IncideInss.Should().BeTrue();
        rubrica.Ativa.Should().BeTrue();
    }

    [Fact] // Persistencia + isolamento por tenant: rubrica de A nao aparece para B.
    public async Task Rubrica_isolada_por_tenant()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            ctx.Rubricas.Add(RubricaFolha.Criar(
                TenantA, Rubrica.De("VENCIMENTO"), "Vencimento", NaturezaRubrica.Provento, Competencia.De(2026, 1), incideInss: true));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantB))
        {
            (await ctx.Rubricas.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact] // Codigo de rubrica e unico por tenant.
    public async Task Rubrica_codigo_unico_por_tenant()
    {
        await using var ctx = CriarContexto(TenantA);
        ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("INSS"), "INSS", NaturezaRubrica.Desconto, Competencia.De(2026, 1), incideInss: true));
        await ctx.SaveChangesAsync();

        ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("INSS"), "INSS dup", NaturezaRubrica.Desconto, Competencia.De(2026, 1)));
        var acao = async () => await ctx.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    // ---------- Tabelas legais (dominio + persistencia) ----------

    [Fact] // INSS exige faixas contiguas terminando no teto.
    public void Inss_rejeita_faixas_nao_contiguas()
    {
        var acao = () => TabelaInss.Criar(
            TenantA, Competencia.De(2026, 1),
            new[] { FaixaProgressiva.De(0m, 1000m, 0.075m), FaixaProgressiva.De(1500m, 2000m, 0.14m) },
            teto: 2000m, baseLegal: "teste");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // Persistencia das tabelas federais semeadas com suas faixas owned + isolamento por tenant.
    public async Task Tabelas_federais_persistem_faixas_e_isolam_por_tenant()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            foreach (var t in TabelasFederaisSeed.Inss(TenantA))
            {
                ctx.TabelasInss.Add(t);
            }

            foreach (var t in TabelasFederaisSeed.Irrf(TenantA))
            {
                ctx.TabelasIrrf.Add(t);
            }

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var inss2026 = await ctx.TabelasInss
                .Include(t => t.Faixas)
                .SingleAsync(t => t.VigenciaInicio == Competencia.De(2026, 1));
            inss2026.Teto.Should().Be(8475.55m);
            inss2026.Faixas.Should().HaveCount(4);

            var irrf = await ctx.TabelasIrrf.Include(t => t.Faixas).ToListAsync();
            irrf.Should().HaveCount(3); // jan-abr/2025, mai-dez/2025 e 2026.
            irrf.SelectMany(t => t.Faixas).Should().NotBeEmpty();

            // IRRF 2026 traz o redutor mensal (Lei 15.270/2025) persistido como owned opcional.
            var irrf2026 = irrf.Single(t => t.VigenciaInicio == Competencia.De(2026, 1));
            irrf2026.Redutor.Should().NotBeNull();
            irrf2026.Redutor!.TetoRedutor.Should().Be(312.89m);
            // Tabelas anteriores nao tem redutor.
            irrf.Single(t => t.VigenciaInicio == Competencia.De(2025, 1)).Redutor.Should().BeNull();
        }

        await using (var ctx = CriarContexto(TenantB))
        {
            (await ctx.TabelasInss.ToListAsync()).Should().BeEmpty();
            (await ctx.TabelasIrrf.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact] // RPPS municipal persiste e e isolada por tenant (fail-closed sem ela no motor).
    public async Task Rpps_municipal_persiste_e_isola()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            ctx.TabelasRpps.Add(TabelaRpps.Criar(
                TenantA, Competencia.De(2026, 1),
                new[] { FaixaProgressiva.De(0m, 999999m, 0.14m) },
                teto: null, baseLegal: "Lei Municipal RPPS (teste)"));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantB))
        {
            (await ctx.TabelasRpps.ToListAsync()).Should().BeEmpty();
        }
    }
}
