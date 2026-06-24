using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;

/// <summary>
/// Cobertura de integracao do agregado <see cref="DiarioClasseAggregate"/>: invariantes, cada
/// transicao da maquina de estados (Aberto -&gt; Apurado) e os cenarios BDD de DiarioClasse.rules.md,
/// sobre SQLite em memoria com auditoria, Outbox e isolamento por tenant.
/// </summary>
public sealed class DiarioClasseFluxoTests : EducacaoTestBase
{
    private static readonly DateOnly DataBase = new(2026, 2, 10);

    private static DiarioClasseAggregate NovoDiario(int cargaHorariaTotal = 800)
        => DiarioClasseAggregate.Abrir(TenantA, new MatriculaId(Guid.NewGuid()), cargaHorariaTotal);

    /// <summary>Lanca frequencias ate atingir o percentual desejado da carga horaria total.</summary>
    private static void PreencherFrequencia(DiarioClasseAggregate diario, int horasPresentes, int horasFaltas)
    {
        if (horasPresentes > 0)
        {
            diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: horasPresentes);
        }

        if (horasFaltas > 0)
        {
            diario.RegistrarFrequencia(DataBase.AddDays(1), presente: false, cargaHorariaAula: horasFaltas);
        }
    }

    // ---------- Invariantes ----------

    [Fact] // I-1: PercentualFrequencia = presencas ponderadas / carga horaria total.
    public void Invariante_1_percentual_frequencia_e_ponderado()
    {
        var diario = NovoDiario(cargaHorariaTotal: 1000);
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 800);
        diario.RegistrarFrequencia(DataBase.AddDays(1), presente: false, cargaHorariaAula: 200);

        diario.PercentualFrequencia.Should().Be(0.8m);
    }

    [Fact] // I-2 + Cenario 2 + B-4: frequencia abaixo de 75% => ReprovadoPorFrequencia.
    public void Invariante_2_frequencia_insuficiente_reprova_por_frequencia()
    {
        var diario = NovoDiario(cargaHorariaTotal: 1000);
        // 700/1000 = 70% < 75%.
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 700);
        diario.RegistrarFrequencia(DataBase.AddDays(1), presente: false, cargaHorariaAula: 300);

        var resultado = diario.ApurarResultado();

        resultado.Should().Be(ResultadoAluno.ReprovadoPorFrequencia);
        diario.Resultado.Should().Be(ResultadoAluno.ReprovadoPorFrequencia);
    }

    [Fact] // I-3 + Cenario 5 + B-6: lancamentos so com diario Aberto.
    public void Invariante_3_lancamentos_exigem_diario_aberto()
    {
        var diario = NovoDiario();
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 800);
        diario.ApurarResultado();

        ((Action)(() => diario.RegistrarFrequencia(DataBase, true, 4))).Should().Throw<InvalidOperationException>();
        ((Action)(() => diario.LancarNota(ComponenteCurricularId.New(), "1Bim", 8m))).Should().Throw<InvalidOperationException>();
        ((Action)(() => diario.RegistrarAula(DataBase, "Conteudo", true))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-4: apuracao exige Aberto, passa a Apurado e emite ResultadoApurado.
    public void Invariante_4_apuracao_passa_a_Apurado_e_emite_evento()
    {
        var diario = NovoDiario();
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 800);

        diario.ApurarResultado();

        diario.Situacao.Should().Be(SituacaoDiario.Apurado);
        diario.DomainEvents.OfType<ResultadoApurado>().Should().ContainSingle();
    }

    [Fact] // I-5 + Cenario 1: frequencia >= 75% e medias suficientes => Aprovado.
    public void Invariante_5_frequencia_e_media_suficientes_aprovam()
    {
        var diario = NovoDiario();
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 800);
        var componente = ComponenteCurricularId.New();
        diario.LancarNota(componente, "1Bim", 7m);
        diario.LancarNota(componente, "2Bim", 8m);

        var resultado = diario.ApurarResultado();

        resultado.Should().Be(ResultadoAluno.Aprovado);
    }

    [Fact] // I-5 + Cenario 3: frequencia >= 75% mas media insuficiente => Reprovado (por nota).
    public void Invariante_5_media_insuficiente_reprova_por_nota()
    {
        var diario = NovoDiario();
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 800);
        var componente = ComponenteCurricularId.New();
        diario.LancarNota(componente, "1Bim", 4m);
        diario.LancarNota(componente, "2Bim", 5m);

        var resultado = diario.ApurarResultado();

        resultado.Should().Be(ResultadoAluno.Reprovado);
    }

    [Fact] // I-6 + Cenario 6: diario Apurado nao admite nova apuracao.
    public void Invariante_6_nova_apuracao_e_bloqueada()
    {
        var diario = NovoDiario();
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 800);
        diario.ApurarResultado();

        ((Action)(() => diario.ApurarResultado())).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8: nota com periodo vazio e rejeitada.
    public void Invariante_8_nota_com_periodo_vazio_e_rejeitada()
    {
        var diario = NovoDiario();

        var acao = () => diario.LancarNota(ComponenteCurricularId.New(), "  ", 7m);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-8: nota com componente vazio e rejeitada.
    public void Invariante_8_nota_com_componente_vazio_e_rejeitada()
    {
        var diario = NovoDiario();

        var acao = () => diario.LancarNota(new ComponenteCurricularId(Guid.Empty), "1Bim", 7m);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-9 + B-8: 200 dias letivos sao a base do cumprimento do calendario.
    public void Invariante_9_dias_letivos_minimos_apuram_cumprimento()
    {
        var diario = NovoDiario();
        for (var i = 0; i < DiarioClasseAggregate.DiasLetivosMinimos; i++)
        {
            diario.RegistrarAula(DataBase.AddDays(i), "Aula", diaLetivo: true);
        }

        diario.RegistrarAula(DataBase.AddDays(300), "Recesso", diaLetivo: false);

        diario.DiasLetivosRegistrados.Should().Be(DiarioClasseAggregate.DiasLetivosMinimos);
        diario.CumpriuCalendario().Should().BeTrue();
    }

    [Fact] // I-9 + B-8: abaixo de 200 dias letivos => calendario nao cumprido.
    public void Invariante_9_abaixo_do_minimo_nao_cumpre_calendario()
    {
        var diario = NovoDiario();
        diario.RegistrarAula(DataBase, "Aula", diaLetivo: true);

        diario.CumpriuCalendario().Should().BeFalse();
    }

    [Fact] // B-3: frequencia exatamente em 75% => atinge o piso de frequencia (limite inclusivo).
    public void Borda_3_frequencia_em_75_porcento_e_inclusiva()
    {
        var diario = NovoDiario(cargaHorariaTotal: 1000);
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 750);
        diario.RegistrarFrequencia(DataBase.AddDays(1), presente: false, cargaHorariaAula: 250);
        var componente = ComponenteCurricularId.New();
        diario.LancarNota(componente, "1Bim", 7m);
        diario.LancarNota(componente, "2Bim", 8m);

        diario.PercentualFrequencia.Should().Be(0.75m);
        var resultado = diario.ApurarResultado();

        // 75% e inclusivo (>= 0.75) e, com rendimento suficiente, aprova.
        resultado.Should().Be(ResultadoAluno.Aprovado);
    }

    [Fact] // ED-1 (W10.6): frequencia suficiente mas ZERO notas => Cursando (nunca Aprovado vacuo).
    public void Frequencia_suficiente_sem_notas_nao_aprova_fica_Cursando()
    {
        var diario = NovoDiario(cargaHorariaTotal: 1000);
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 800); // 80% >= 75%.

        diario.PossuiRendimentoLancado.Should().BeFalse();
        var resultado = diario.ApurarResultado();

        // Fail-closed: sem rendimento lancado nao ha base avaliativa para concluir aprovacao.
        resultado.Should().Be(ResultadoAluno.Cursando);
        resultado.Should().NotBe(ResultadoAluno.Aprovado);
    }

    [Fact] // ED-1 (W10.6): frequencia insuficiente e ZERO notas => ReprovadoPorFrequencia (fato objetivo).
    public void Frequencia_insuficiente_sem_notas_reprova_por_frequencia()
    {
        var diario = NovoDiario(cargaHorariaTotal: 1000);
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 700); // 70% < 75%.
        diario.RegistrarFrequencia(DataBase.AddDays(1), presente: false, cargaHorariaAula: 300);

        // Reprovacao por frequencia independe de nota: precede a regra de Cursando.
        diario.ApurarResultado().Should().Be(ResultadoAluno.ReprovadoPorFrequencia);
    }

    [Fact] // B-1: carga horaria total <= 0 em Abrir e rejeitada.
    public void Borda_1_carga_horaria_total_nao_positiva_e_rejeitada()
    {
        var acao = () => DiarioClasseAggregate.Abrir(TenantA, new MatriculaId(Guid.NewGuid()), 0);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // B-2: carga horaria da aula <= 0 em RegistrarFrequencia e rejeitada.
    public void Borda_2_carga_horaria_aula_nao_positiva_e_rejeitada()
    {
        var diario = NovoDiario();

        var acao = () => diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 0);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // (nenhum) --Abrir--> Aberto.
    public void Transicao_abrir_para_Aberto()
    {
        var diario = NovoDiario();

        diario.Situacao.Should().Be(SituacaoDiario.Aberto);
        diario.Resultado.Should().BeNull();
    }

    [Fact] // Aberto --RegistrarFrequencia--> Aberto, emite FrequenciaRegistrada.
    public void Transicao_registrar_frequencia_mantem_Aberto_e_emite_evento()
    {
        var diario = NovoDiario();

        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 4);

        diario.Situacao.Should().Be(SituacaoDiario.Aberto);
        diario.Frequencias.Should().ContainSingle();
        diario.DomainEvents.OfType<FrequenciaRegistrada>().Should().ContainSingle();
    }

    [Fact] // Aberto --LancarNota--> Aberto, emite NotaLancada.
    public void Transicao_lancar_nota_mantem_Aberto_e_emite_evento()
    {
        var diario = NovoDiario();

        diario.LancarNota(ComponenteCurricularId.New(), "1Bim", 8m);

        diario.Situacao.Should().Be(SituacaoDiario.Aberto);
        diario.Notas.Should().ContainSingle();
        diario.DomainEvents.OfType<NotaLancada>().Should().ContainSingle();
    }

    [Fact] // Aberto --RegistrarAula--> Aberto (sem evento).
    public void Transicao_registrar_aula_mantem_Aberto()
    {
        var diario = NovoDiario();

        diario.RegistrarAula(DataBase, "Conteudo", diaLetivo: true);

        diario.Situacao.Should().Be(SituacaoDiario.Aberto);
        diario.Aulas.Should().ContainSingle();
    }

    [Fact] // Aberto --ApurarResultado--> Apurado.
    public void Transicao_apurar_de_Aberto_para_Apurado()
    {
        var diario = NovoDiario();
        diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 800);

        diario.ApurarResultado();

        diario.Situacao.Should().Be(SituacaoDiario.Apurado);
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // I-7 + Cenario 7: vinculo 1-1 com a matricula (indice unico (TenantId, MatriculaId)).
    public async Task Cenario_7_vinculo_1_1_com_matricula_e_unico()
    {
        var matriculaId = new MatriculaId(Guid.NewGuid());
        await using var contexto = CriarContexto(TenantA);
        contexto.DiariosClasse.Add(DiarioClasseAggregate.Abrir(TenantA, matriculaId, 800));
        await contexto.SaveChangesAsync();

        contexto.DiariosClasse.Add(DiarioClasseAggregate.Abrir(TenantA, matriculaId, 800));
        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // Cenario 1/4: apuracao persiste com filhos e registra trilha de auditoria.
    public async Task Cenario_4_lancamentos_e_apuracao_persistem_com_auditoria()
    {
        DiarioClasseId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var diario = NovoDiario();
            id = diario.Id;
            diario.RegistrarFrequencia(DataBase, presente: true, cargaHorariaAula: 800);
            diario.LancarNota(ComponenteCurricularId.New(), "1Bim", 9m);
            diario.RegistrarAula(DataBase, "Introducao", diaLetivo: true);
            contexto.DiariosClasse.Add(diario);
            await contexto.SaveChangesAsync();

            diario.ApurarResultado();
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var diario = await contexto.DiariosClasse
                .Include(d => d.Frequencias)
                .Include(d => d.Notas)
                .Include(d => d.Aulas)
                .SingleAsync(d => d.Id == id);

            diario.Situacao.Should().Be(SituacaoDiario.Apurado);
            diario.Resultado.Should().Be(ResultadoAluno.Aprovado);
            diario.TenantId.Should().Be(TenantA);
            diario.Frequencias.Should().ContainSingle();
            diario.Notas.Should().ContainSingle();
            diario.Aulas.Should().ContainSingle();

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes do diario");
        }
    }

    [Fact] // B-12 + Cenario 9: consulta e tenant-scoped (Global Query Filter).
    public async Task Cenario_9_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.DiariosClasse.Add(NovoDiario());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.DiariosClasse.ToListAsync()).Should().BeEmpty();
        }
    }
}
