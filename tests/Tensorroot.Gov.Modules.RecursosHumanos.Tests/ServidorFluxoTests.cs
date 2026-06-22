using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Servidor"/>: invariantes, cada transicao da
/// maquina de estados (Nomeacao -> Posse -> Exercicio -> Estabilidade/Afastamento/Desligamento)
/// e os cenarios BDD de Servidor.rules.md, sobre SQLite em memoria com auditoria e isolamento
/// por tenant.
/// </summary>
public sealed class ServidorFluxoTests : RecursosHumanosTestBase
{
    private static readonly DateOnly Nomeacao = new(2026, 1, 5);

    private static DadosPessoais NovosDados()
        => DadosPessoais.Criar("Maria da Silva", new DateOnly(1990, 3, 20));

    private static Servidor NovoServidor(
        RegimePrevidenciario regime = RegimePrevidenciario.Rpps,
        string matricula = "MAT-0001",
        string cpf = "52998224725")
        => Servidor.Admitir(
            TenantA,
            Cpf.Create(cpf),
            Matricula.De(matricula),
            NovosDados(),
            CargoId.New(),
            regime,
            Nomeacao);

    /// <summary>Leva o servidor ate EmExercicio (posse + exercicio dentro do prazo).</summary>
    private static Servidor NovoServidorEmExercicio(
        RegimePrevidenciario regime = RegimePrevidenciario.Rpps,
        DateOnly? dataExercicio = null,
        string matricula = "MAT-0001")
    {
        var servidor = NovoServidor(regime, matricula);
        servidor.RegistrarPosse(Nomeacao.AddDays(10));
        servidor.IniciarExercicio(dataExercicio ?? Nomeacao.AddDays(15));
        return servidor;
    }

    // ---------- Invariantes ----------

    [Fact] // I-9 + Cenario 1: admissao nasce Nomeado e emite ServidorAdmitido.
    public void Invariante_9_admissao_nasce_nomeado_e_emite_evento()
    {
        var servidor = NovoServidor();

        servidor.Situacao.Should().Be(SituacaoServidor.Nomeado);
        servidor.DomainEvents.OfType<ServidorAdmitido>().Should().ContainSingle();
    }

    [Fact] // I-1 + B-1: admissao com CPF nulo e rejeitada.
    public void Invariante_1_admissao_com_cpf_nulo_e_rejeitada()
    {
        var acao = () => Servidor.Admitir(
            TenantA, null!, Matricula.De("MAT"), NovosDados(), CargoId.New(),
            RegimePrevidenciario.Rpps, Nomeacao);

        acao.Should().Throw<ArgumentNullException>();
    }

    [Fact] // I-1 + B-1: admissao com matricula nula e rejeitada.
    public void Invariante_1_admissao_com_matricula_nula_e_rejeitada()
    {
        var acao = () => Servidor.Admitir(
            TenantA, Cpf.Create("52998224725"), null!, NovosDados(), CargoId.New(),
            RegimePrevidenciario.Rpps, Nomeacao);

        acao.Should().Throw<ArgumentNullException>();
    }

    [Fact] // I-1 + B-1: admissao com dados pessoais nulos e rejeitada.
    public void Invariante_1_admissao_com_dados_nulos_e_rejeitada()
    {
        var acao = () => Servidor.Admitir(
            TenantA, Cpf.Create("52998224725"), Matricula.De("MAT"), null!, CargoId.New(),
            RegimePrevidenciario.Rpps, Nomeacao);

        acao.Should().Throw<ArgumentNullException>();
    }

    [Fact] // I-3 + Cenario 4: exercicio sem posse e rejeitado.
    public void Invariante_3_exercicio_sem_posse_e_rejeitado()
    {
        var servidor = NovoServidor();

        var acao = () => servidor.IniciarExercicio(Nomeacao.AddDays(20));

        acao.Should().Throw<InvalidOperationException>();
        servidor.Situacao.Should().Be(SituacaoServidor.Nomeado);
    }

    [Fact] // I-4 + Cenario 2 + B-4: posse alem do prazo legal caduca (sem PosseRegistrada).
    public void Invariante_4_posse_alem_do_prazo_caduca_sem_evento()
    {
        var servidor = NovoServidor();

        var acao = () => servidor.RegistrarPosse(Nomeacao.AddDays(Servidor.PrazoPosseDias + 1));

        acao.Should().Throw<InvalidOperationException>();
        servidor.Situacao.Should().Be(SituacaoServidor.Nomeado);
        servidor.DomainEvents.OfType<PosseRegistrada>().Should().BeEmpty();
    }

    [Fact] // B-4: posse no exato limite do prazo legal e aceita.
    public void Borda_4_posse_no_limite_do_prazo_e_aceita()
    {
        var servidor = NovoServidor();

        servidor.RegistrarPosse(Nomeacao.AddDays(Servidor.PrazoPosseDias));

        servidor.Situacao.Should().Be(SituacaoServidor.Empossado);
    }

    [Fact] // I-5 + Cenario 6: estabilidade negada antes de 3 anos.
    public void Invariante_5_estabilidade_negada_antes_de_tres_anos()
    {
        var inicio = new DateOnly(2024, 1, 10);
        var servidor = NovoServidorEmExercicio(RegimePrevidenciario.Rpps, inicio);

        // 2 anos e 364 dias -> negado.
        var acao = () => servidor.ConcederEstabilidade(inicio.AddYears(3).AddDays(-1));

        acao.Should().Throw<InvalidOperationException>();
        servidor.Situacao.Should().Be(SituacaoServidor.EmExercicio);
    }

    [Fact] // I-5 + B-6: empregado publico (RGPS) nao adquire estabilidade.
    public void Invariante_5_rgps_nao_adquire_estabilidade()
    {
        var inicio = new DateOnly(2020, 1, 10);
        var servidor = NovoServidorEmExercicio(RegimePrevidenciario.Rgps, inicio);

        var acao = () => servidor.ConcederEstabilidade(inicio.AddYears(5));

        acao.Should().Throw<InvalidOperationException>();
        servidor.Situacao.Should().Be(SituacaoServidor.EmExercicio);
    }

    [Fact] // I-7 + Cenario 8: afastamento fora de atividade plena (Empossado) e bloqueado.
    public void Invariante_7_afastamento_fora_de_atividade_e_bloqueado()
    {
        var servidor = NovoServidor();
        servidor.RegistrarPosse(Nomeacao.AddDays(5));

        var acao = () => servidor.RegistrarAfastamento(Nomeacao.AddDays(6), null, "Licenca");

        acao.Should().Throw<InvalidOperationException>();
        servidor.Situacao.Should().Be(SituacaoServidor.Empossado);
    }

    [Fact] // B-7: afastamento com Fim < Inicio e rejeitado.
    public void Borda_7_afastamento_com_fim_anterior_ao_inicio_e_rejeitado()
    {
        var servidor = NovoServidorEmExercicio();

        var acao = () => servidor.RegistrarAfastamento(new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 1), "Licenca");

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-8 + Cenario 10: servidor desligado e terminal (nenhuma transicao).
    public void Invariante_8_servidor_desligado_e_terminal()
    {
        var servidor = NovoServidorEmExercicio();
        servidor.Desligar(new DateOnly(2026, 6, 1), "Exoneracao");

        servidor.Situacao.Should().Be(SituacaoServidor.Desligado);
        ((Action)(() => servidor.RegistrarPosse(Nomeacao.AddDays(5)))).Should().Throw<InvalidOperationException>();
        ((Action)(() => servidor.IniciarExercicio(Nomeacao.AddDays(20)))).Should().Throw<InvalidOperationException>();
        ((Action)(() => servidor.ConcederEstabilidade(new DateOnly(2030, 1, 1)))).Should().Throw<InvalidOperationException>();
        ((Action)(() => servidor.RegistrarAfastamento(new DateOnly(2026, 7, 1), null, "Licenca"))).Should().Throw<InvalidOperationException>();
        ((Action)(() => servidor.Desligar(new DateOnly(2026, 7, 1), "De novo"))).Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 3: Nomeado --Posse--> Empossado --Exercicio--> EmExercicio.
    public void Transicao_sequencia_nomeacao_posse_exercicio()
    {
        var servidor = NovoServidor();

        servidor.RegistrarPosse(Nomeacao.AddDays(10));
        servidor.Situacao.Should().Be(SituacaoServidor.Empossado);
        servidor.DataPosse.Should().Be(Nomeacao.AddDays(10));
        servidor.DomainEvents.OfType<PosseRegistrada>().Should().ContainSingle();

        servidor.IniciarExercicio(Nomeacao.AddDays(15));
        servidor.Situacao.Should().Be(SituacaoServidor.EmExercicio);
        servidor.DataExercicio.Should().Be(Nomeacao.AddDays(15));
        servidor.DomainEvents.OfType<ExercicioIniciado>().Should().ContainSingle();
    }

    [Fact] // I-5 + Cenario 5: estabilidade apos 3 anos (efetivo) -> Estavel.
    public void Transicao_estabilidade_apos_tres_anos_para_Estavel()
    {
        var inicio = new DateOnly(2023, 1, 10);
        var servidor = NovoServidorEmExercicio(RegimePrevidenciario.Rpps, inicio);

        servidor.ConcederEstabilidade(inicio.AddYears(3));

        servidor.Situacao.Should().Be(SituacaoServidor.Estavel);
        servidor.DataEstabilidade.Should().Be(inicio.AddYears(3));
        servidor.DomainEvents.OfType<EstabilidadeConcedida>().Should().ContainSingle();
    }

    [Fact] // Cenario 7: EmExercicio --Afastamento--> Afastado, emite AfastamentoRegistrado.
    public void Transicao_afastamento_de_EmExercicio_para_Afastado()
    {
        var servidor = NovoServidorEmExercicio();

        servidor.RegistrarAfastamento(new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 1), "Licenca medica");

        servidor.Situacao.Should().Be(SituacaoServidor.Afastado);
        servidor.DomainEvents.OfType<AfastamentoRegistrado>().Should().ContainSingle();
    }

    [Fact] // Afastado --RetornarDeAfastamento--> EmExercicio.
    public void Transicao_retornar_de_afastamento_para_EmExercicio()
    {
        var servidor = NovoServidorEmExercicio();
        servidor.RegistrarAfastamento(new DateOnly(2026, 5, 1), null, "Licenca");

        servidor.RetornarDeAfastamento();

        servidor.Situacao.Should().Be(SituacaoServidor.EmExercicio);
    }

    [Fact] // B-8: retornar de afastamento fora de Afastado e rejeitado.
    public void Borda_8_retornar_fora_de_afastado_e_rejeitado()
    {
        var servidor = NovoServidorEmExercicio();

        var acao = servidor.RetornarDeAfastamento;

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // B-9: desligar servidor Afastado e permitido (afastado nao e terminal).
    public void Borda_9_desligar_servidor_afastado_e_permitido()
    {
        var servidor = NovoServidorEmExercicio();
        servidor.RegistrarAfastamento(new DateOnly(2026, 5, 1), null, "Licenca");

        servidor.Desligar(new DateOnly(2026, 6, 1), "Aposentadoria");

        servidor.Situacao.Should().Be(SituacaoServidor.Desligado);
        servidor.DomainEvents.OfType<ServidorDesligado>().Should().ContainSingle();
    }

    [Fact] // I-10 + Cenario 9: desligamento emite ServidorDesligado.
    public void Transicao_desligar_de_EmExercicio_para_Desligado()
    {
        var servidor = NovoServidorEmExercicio();

        servidor.Desligar(new DateOnly(2026, 6, 1), "Exoneracao");

        servidor.Situacao.Should().Be(SituacaoServidor.Desligado);
        servidor.DataDesligamento.Should().Be(new DateOnly(2026, 6, 1));
        servidor.DomainEvents.OfType<ServidorDesligado>().Should().ContainSingle()
            .Which.Motivo.Should().Be("Exoneracao");
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 1 (persistencia): admissao + desligamento persistem com auditoria.
    public async Task Fluxo_de_desligamento_persiste_com_auditoria()
    {
        ServidorId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var servidor = NovoServidorEmExercicio();
            id = servidor.Id;
            contexto.Servidores.Add(servidor);
            await contexto.SaveChangesAsync();

            servidor.Desligar(new DateOnly(2026, 6, 1), "Exoneracao");
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var servidor = await contexto.Servidores.SingleAsync(s => s.Id == id);
            servidor.Situacao.Should().Be(SituacaoServidor.Desligado);
            servidor.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes do servidor");
        }
    }

    [Fact] // Cenario 11: matricula unica por tenant — duplicar viola o indice unico.
    public async Task Cenario_11_matricula_duplicada_no_tenant_e_rejeitada()
    {
        await using var contexto = CriarContexto(TenantA);
        contexto.Servidores.Add(NovoServidor(matricula: "MAT-DUP", cpf: "52998224725"));
        await contexto.SaveChangesAsync();

        contexto.Servidores.Add(NovoServidor(matricula: "MAT-DUP", cpf: "11144477735"));

        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // Cenario 12: consulta de servidores e isolada por tenant (Global Query Filter).
    public async Task Cenario_12_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Servidores.Add(NovoServidor());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Servidores.ToListAsync()).Should().BeEmpty();
        }
    }
}
