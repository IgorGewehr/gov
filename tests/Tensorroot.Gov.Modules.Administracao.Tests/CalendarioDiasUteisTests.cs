using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Tempo;
using Tensorroot.Gov.SharedKernel.Tempo;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Cobertura do servico transversal de DIAS UTEIS / PRAZOS LEGAIS (W9.1 — promove o calendario a servico
/// compartilhado). Regra de negocio critica de prazo legal (CLAUDE.md §12): feriados moveis (Computus),
/// virada de ano, ano bissexto, feriado em fim de semana, determinismo, convencao "inicio nao conta",
/// VO <see cref="PrazoLegal"/> e isolamento por tenant. Usa fake deterministico de
/// <see cref="IFeriadosTenantProvider"/> (sem relogio, sem I/O) e tambem o provider REAL contra config.
/// </summary>
public sealed class CalendarioDiasUteisTests
{
    // === Fake deterministico de feriados (conjunto fixo por ano) ===
    private sealed class FeriadosFake(Func<int, IReadOnlySet<DateOnly>> fonte) : IFeriadosTenantProvider
    {
        public IReadOnlySet<DateOnly> FeriadosDoAno(int ano) => fonte(ano);
    }

    private static CalendarioDiasUteis ComFeriados(params DateOnly[] feriados)
    {
        var conjunto = new HashSet<DateOnly>(feriados);
        return new CalendarioDiasUteis(new FeriadosFake(_ => conjunto));
    }

    private static CalendarioDiasUteis SemFeriados()
        => new(new FeriadosFake(_ => new HashSet<DateOnly>()));

    // ---------- 1) Anos bissextos ----------

    [Fact]
    public void AdicionarDiasUteis_atravessa_29_02_em_ano_bissexto()
    {
        // 2024 e bissexto: 28/02 (qua) -> 29/02 (qui) existe e e dia util.
        var cal = SemFeriados();
        // 27/02/2024 e terca; +3 d.u. -> qua 28, qui 29, sex 01/03.
        cal.AdicionarDiasUteis(new DateOnly(2024, 2, 27), 3).Should().Be(new DateOnly(2024, 3, 1));
    }

    [Fact]
    public void DiasUteisEntre_conta_certo_cruzando_fevereiro_comum_2025()
    {
        // 2025 nao e bissexto: 28/02 (sex). Entre 24/02 (seg) e 03/03 (seg): ter,qua,qui,sex,seg = 5 d.u.
        var cal = SemFeriados();
        cal.DiasUteisEntre(new DateOnly(2025, 2, 24), new DateOnly(2025, 3, 3)).Should().Be(5);
    }

    // ---------- 2) Virada de ano ----------

    [Fact]
    public void AdicionarDiasUteis_pula_01_01_na_virada_de_ano()
    {
        // 01/01/2027 (sex) e feriado nacional fixo. Partindo de 28/12/2026 (seg) +5 d.u.:
        // ter 29, qua 30, qui 31, [pula sex 01/01 feriado + sab/dom], seg 04/01, ter 05/01 = 5 d.u. -> 05/01/2027.
        var cal = ComFeriados(new DateOnly(2027, 1, 1));
        cal.AdicionarDiasUteis(new DateOnly(2026, 12, 28), 5).Should().Be(new DateOnly(2027, 1, 5));
    }

    // ---------- 3) Feriado em fim de semana / rolagem corrida ----------

    [Fact]
    public void Feriado_que_cai_no_sabado_nao_soma_dia_extra()
    {
        // 25/12/2027 e sabado. EhDiaUtil deve ser false (sem dupla contagem).
        var natalSabado = new DateOnly(2027, 12, 25);
        natalSabado.DayOfWeek.Should().Be(DayOfWeek.Saturday);
        var cal = ComFeriados(natalSabado);
        cal.EhDiaUtil(natalSabado).Should().BeFalse();
    }

    [Fact]
    public void ProximoDiaUtil_rola_vencimento_corrido_que_cai_em_feriado()
    {
        // Vencimento corrido em 01/05 (sex) feriado -> rola para o proximo util.
        var cal = ComFeriados(new DateOnly(2026, 5, 1)); // 01/05/2026 e sexta-feira.
        new DateOnly(2026, 5, 1).DayOfWeek.Should().Be(DayOfWeek.Friday);
        cal.ProximoDiaUtil(new DateOnly(2026, 5, 1)).Should().Be(new DateOnly(2026, 5, 4)); // segunda.
    }

    // ---------- 4) Feriados moveis (Computus) ----------

    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    public void Pascoa_bate_tabela_conhecida_sem_hardcode_de_ano(int ano, int mes, int dia)
        => FeriadosMoveis.Pascoa(ano).Should().Be(new DateOnly(ano, mes, dia));

    [Fact]
    public void Derivados_da_pascoa_em_2026_carnaval_em_fevereiro_corpus_em_junho()
    {
        // 2026: Pascoa 05/04. Carnaval (terca) = -47d = 17/02 (fevereiro); Corpus = +60d = 04/06 (junho).
        FeriadosMoveis.TercaCarnaval(2026).Should().Be(new DateOnly(2026, 2, 17));
        FeriadosMoveis.SextaFeiraSanta(2026).Should().Be(new DateOnly(2026, 4, 3));
        FeriadosMoveis.CorpusChristi(2026).Should().Be(new DateOnly(2026, 6, 4));
    }

    // ---------- 5) Determinismo / reprodutibilidade ----------

    [Fact]
    public void AdicionarDiasUteis_e_deterministico_mesma_entrada_mesma_saida()
    {
        var cal = SemFeriados();
        var inicio = new DateOnly(2026, 6, 15);
        var r1 = cal.AdicionarDiasUteis(inicio, 13);
        var r2 = cal.AdicionarDiasUteis(inicio, 13);
        r1.Should().Be(r2);
    }

    // ---------- 6) Convencao "inicio nao conta" ----------

    [Fact]
    public void AdicionarDiasUteis_zero_e_idempotente()
        => SemFeriados().AdicionarDiasUteis(new DateOnly(2026, 6, 15), 0).Should().Be(new DateOnly(2026, 6, 15));

    [Fact]
    public void DiasUteisEntre_segunda_a_sexta_da_mesma_semana_e_quatro()
    {
        // 15/06/2026 e segunda. Exclusivo no inicio, inclusivo no fim: ter,qua,qui,sex = 4.
        new DateOnly(2026, 6, 15).DayOfWeek.Should().Be(DayOfWeek.Monday);
        SemFeriados().DiasUteisEntre(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 19)).Should().Be(4);
    }

    [Fact]
    public void ProximoDiaUtil_em_dia_util_e_idempotente()
        => SemFeriados().ProximoDiaUtil(new DateOnly(2026, 6, 15)).Should().Be(new DateOnly(2026, 6, 15));

    [Fact]
    public void DiasUteisEntre_invertido_e_negativo()
        => SemFeriados().DiasUteisEntre(new DateOnly(2026, 6, 19), new DateOnly(2026, 6, 15)).Should().Be(-4);

    // ---------- 7) VO PrazoLegal ----------

    [Fact]
    public void PrazoLegal_dias_uteis_resolve_vencimento_via_calendario()
    {
        var cal = SemFeriados();
        // 15/06/2026 (seg) +20 d.u. (sem feriados) = 13/07/2026 (seg).
        var prazo = PrazoLegal.Criar(new DateOnly(2026, 6, 15), 20, UnidadePrazo.DiasUteis, "Lei 14.133/2021 art. 94", cal);
        prazo.Vencimento.Should().Be(new DateOnly(2026, 7, 13));
        prazo.NormaFonte.Should().Be("Lei 14.133/2021 art. 94");
    }

    [Fact]
    public void PrazoLegal_corridos_rola_para_dia_util()
    {
        var cal = ComFeriados();
        // 19/06/2026 (sex) +1 corrido = 20/06 (sab) -> rola para 22/06 (seg).
        var prazo = PrazoLegal.Criar(new DateOnly(2026, 6, 19), 1, UnidadePrazo.DiasCorridos, "art. 110", cal);
        prazo.Vencimento.Should().Be(new DateOnly(2026, 6, 22));
    }

    [Fact]
    public void PrazoLegal_vencido_e_avencer_nas_bordas()
    {
        var cal = SemFeriados();
        var prazo = PrazoLegal.Criar(new DateOnly(2026, 6, 15), 5, UnidadePrazo.DiasUteis, "norma", cal);
        var venc = prazo.Vencimento;
        prazo.AVencer(venc).Should().BeTrue();          // hoje == vencimento -> ainda a vencer.
        prazo.Vencido(venc).Should().BeFalse();
        prazo.Vencido(venc.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void PrazoLegal_prorrogar_encadeia_e_sic_20_mais_10()
    {
        var cal = SemFeriados();
        // e-SIC: 20 d.u. (LAI art. 11) + prorrogacao 10 d.u. (art. 11 §2).
        var inicial = PrazoLegal.Criar(new DateOnly(2026, 6, 15), 20, UnidadePrazo.DiasUteis, "LAI art. 11", cal);
        var prorrogado = inicial.Prorrogar(10, "LAI art. 11 §2", cal);
        // 13/07 (vencimento inicial) +10 d.u. = 27/07/2026 (seg).
        inicial.Vencimento.Should().Be(new DateOnly(2026, 7, 13));
        prorrogado.Vencimento.Should().Be(new DateOnly(2026, 7, 27));
    }

    [Fact]
    public void PrazoLegal_igualdade_estrutural()
    {
        var cal = SemFeriados();
        var a = PrazoLegal.Criar(new DateOnly(2026, 6, 15), 5, UnidadePrazo.DiasUteis, "norma", cal);
        var b = PrazoLegal.Criar(new DateOnly(2026, 6, 15), 5, UnidadePrazo.DiasUteis, "norma", cal);
        a.Should().Be(b);
    }

    [Fact]
    public void PrazoLegal_quantidade_negativa_lanca()
    {
        var cal = SemFeriados();
        var acao = () => PrazoLegal.Criar(new DateOnly(2026, 6, 15), -1, UnidadePrazo.DiasUteis, "norma", cal);
        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------- 8) Multi-tenant (fonte de feriados por tenant) ----------

    [Fact]
    public void Tenant_com_carnaval_e_sem_carnaval_dao_vencimentos_diferentes()
    {
        // 2026: Carnaval terca 17/02, segunda 16/02 (ambos dias uteis na semana).
        var carnaval = new[] { FeriadosMoveis.SegundaCarnaval(2026), FeriadosMoveis.TercaCarnaval(2026) };
        var comCarnaval = new CalendarioDiasUteis(new FeriadosFake(_ => new HashSet<DateOnly>(carnaval)));
        var semCarnaval = new CalendarioDiasUteis(new FeriadosFake(_ => new HashSet<DateOnly>()));

        // Partindo de 13/02/2026 (sex), +3 d.u.
        var inicio = new DateOnly(2026, 2, 13);
        var vCom = comCarnaval.AdicionarDiasUteis(inicio, 3);
        var vSem = semCarnaval.AdicionarDiasUteis(inicio, 3);
        vCom.Should().NotBe(vSem, "o tenant que adota o Carnaval tem mais dias nao-uteis -> vencimento posterior");
        vCom.Should().BeAfter(vSem);
    }

    // ---------- 9) Provider REAL (config + cache) por tenant ----------

    private sealed class TenantFixo(Guid id) : ITenantContext
    {
        public Guid TenantId => id;
        public bool HasTenant => true;
    }

    private static IConfiguration ConfigComFeriados(params (string chave, string valor)[] pares)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var (chave, valor) in pares)
        {
            dict[chave] = valor;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void FeriadosTenantProvider_inclui_fixos_moveis_e_municipais_da_config()
    {
        var config = ConfigComFeriados(
            ("Tempo:Feriados:AdotaCorpusChristi", "true"),
            ("Tempo:Feriados:MunicipaisFixos:0", "08-15")); // padroeiro municipal recorrente.
        var provider = new FeriadosTenantProvider(config, new TenantFixo(Guid.NewGuid()), new MemoryCache(new MemoryCacheOptions()));

        var feriados = provider.FeriadosDoAno(2026);
        feriados.Should().Contain(new DateOnly(2026, 1, 1));    // fixo: Confraternizacao.
        feriados.Should().Contain(new DateOnly(2026, 11, 20));  // fixo: Consciencia Negra (Lei 14.759/2023).
        feriados.Should().Contain(FeriadosMoveis.SextaFeiraSanta(2026)); // movel consolidado.
        feriados.Should().Contain(FeriadosMoveis.CorpusChristi(2026));   // movel adotado.
        feriados.Should().Contain(new DateOnly(2026, 8, 15));   // municipal da config.
    }

    [Fact]
    public void FeriadosTenantProvider_isola_por_tenant_carnaval_so_no_tenant_que_adota()
    {
        var configA = ConfigComFeriados(("Tempo:Feriados:AdotaCarnaval", "true"));
        var configB = ConfigComFeriados(); // tenant B nao adota Carnaval.
        var cache = new MemoryCache(new MemoryCacheOptions());

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var provA = new FeriadosTenantProvider(configA, new TenantFixo(tenantA), cache);
        var provB = new FeriadosTenantProvider(configB, new TenantFixo(tenantB), cache);

        provA.FeriadosDoAno(2026).Should().Contain(FeriadosMoveis.TercaCarnaval(2026));
        provB.FeriadosDoAno(2026).Should().NotContain(FeriadosMoveis.TercaCarnaval(2026));
    }

    [Fact]
    public void FeriadosTenantProvider_nao_adota_carnaval_por_default()
    {
        var provider = new FeriadosTenantProvider(ConfigComFeriados(), new TenantFixo(Guid.NewGuid()), new MemoryCache(new MemoryCacheOptions()));
        provider.FeriadosDoAno(2026).Should().NotContain(FeriadosMoveis.TercaCarnaval(2026));
    }

    // ---------- T-1: isolamento REAL multi-tenant numa MESMA IConfiguration (singleton de producao) ----------

    [Fact]
    public void FeriadosTenantProvider_isola_municipais_por_tenant_na_mesma_configuracao()
    {
        // Cenario de PRODUCAO: um unico IConfiguration (singleton) compartilhado por todos os tenants.
        // Cada tenant tem sua sub-secao Tempo:Feriados:Tenants:{tenantId} com seu padroeiro/aniversario.
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var config = ConfigComFeriados(
            ($"Tempo:Feriados:Tenants:{tenantA:D}:MunicipaisFixos:0", "08-15"), // padroeiro do tenant A.
            ($"Tempo:Feriados:Tenants:{tenantA:D}:AdotaCarnaval", "true"),
            ($"Tempo:Feriados:Tenants:{tenantB:D}:MunicipaisFixos:0", "06-13")); // padroeiro do tenant B.
        var cache = new MemoryCache(new MemoryCacheOptions());

        var provA = new FeriadosTenantProvider(config, new TenantFixo(tenantA), cache);
        var provB = new FeriadosTenantProvider(config, new TenantFixo(tenantB), cache);

        var feriadosA = provA.FeriadosDoAno(2026);
        var feriadosB = provB.FeriadosDoAno(2026);

        // Municipal e' do tenant dono — nao vaza para o outro.
        feriadosA.Should().Contain(new DateOnly(2026, 8, 15));
        feriadosA.Should().NotContain(new DateOnly(2026, 6, 13));
        feriadosB.Should().Contain(new DateOnly(2026, 6, 13));
        feriadosB.Should().NotContain(new DateOnly(2026, 8, 15));

        // Facultativo (Carnaval) tambem isolado por tenant.
        feriadosA.Should().Contain(FeriadosMoveis.TercaCarnaval(2026));
        feriadosB.Should().NotContain(FeriadosMoveis.TercaCarnaval(2026));
    }

    [Fact]
    public void FeriadosTenantProvider_cai_para_secao_raiz_no_ente_unico_sem_subsecao()
    {
        // Single-tenant (piloto): sem sub-secao do tenant, vale a secao raiz Tempo:Feriados (compat).
        var config = ConfigComFeriados(("Tempo:Feriados:MunicipaisFixos:0", "08-15"));
        var provider = new FeriadosTenantProvider(config, new TenantFixo(Guid.NewGuid()), new MemoryCache(new MemoryCacheOptions()));

        provider.FeriadosDoAno(2026).Should().Contain(new DateOnly(2026, 8, 15));
    }

    [Fact]
    public void FeriadosTenantProvider_subsecao_do_tenant_tem_precedencia_sobre_raiz()
    {
        // Havendo sub-secao propria, ela e' a fonte de verdade — a raiz nao "vaza" para o tenant.
        var tenant = Guid.NewGuid();
        var config = ConfigComFeriados(
            ("Tempo:Feriados:MunicipaisFixos:0", "08-15"),                         // raiz (ente unico legado).
            ($"Tempo:Feriados:Tenants:{tenant:D}:MunicipaisFixos:0", "06-13"));    // tenant especifico.
        var provider = new FeriadosTenantProvider(config, new TenantFixo(tenant), new MemoryCache(new MemoryCacheOptions()));

        var feriados = provider.FeriadosDoAno(2026);
        feriados.Should().Contain(new DateOnly(2026, 6, 13));     // do proprio tenant.
        feriados.Should().NotContain(new DateOnly(2026, 8, 15));  // raiz nao vaza quando ha sub-secao.
    }
}
