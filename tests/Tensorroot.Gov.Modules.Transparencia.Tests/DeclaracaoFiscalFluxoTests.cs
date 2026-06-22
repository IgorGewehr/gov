using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.Events;
using Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="DeclaracaoFiscal"/>: invariantes, cada transicao
/// da maquina de estados, os cenarios BDD e casos de borda de DeclaracaoFiscal.rules.md e o
/// isolamento por tenant — sobre SQLite em memoria com auditoria, Outbox e Global Query Filter.
/// </summary>
public sealed class DeclaracaoFiscalFluxoTests : TransparenciaTestBase
{
    private static MatrizSaldos MatrizBalanceada(decimal valor = 1000m)
        => MatrizSaldos.Montar(
        [
            LinhaContabil.Criar("1.1.1.1.01.00", NaturezaSaldo.Devedor, ValorMonetario.De(valor)),
            LinhaContabil.Criar("2.1.1.1.01.00", NaturezaSaldo.Credor, ValorMonetario.De(valor)),
        ]);

    private static MatrizSaldos MatrizDesbalanceada()
        => MatrizSaldos.Montar(
        [
            LinhaContabil.Criar("1.1.1.1.01.00", NaturezaSaldo.Devedor, ValorMonetario.De(1000m)),
            LinhaContabil.Criar("2.1.1.1.01.00", NaturezaSaldo.Credor, ValorMonetario.De(900m)),
        ]);

    private static DeclaracaoFiscal NovaMsc(Guid? tenant = null, int mes = 5)
        => DeclaracaoFiscal.ConsolidarMatriz(
            tenant ?? TenantA,
            TipoDeclaracaoFiscal.Msc,
            2026,
            Competencia.De(2026, mes),
            bimestre: null,
            quadrimestre: null,
            new DateOnly(2026, 6, 30),
            new DateOnly(2026, 6, 5),
            MatrizBalanceada());

    private static DeclaracaoFiscal NovoRreo()
        => DeclaracaoFiscal.ConsolidarMatriz(
            TenantA,
            TipoDeclaracaoFiscal.Rreo,
            2026,
            competencia: null,
            Bimestre.De(2026, 2),
            quadrimestre: null,
            new DateOnly(2026, 5, 30),
            new DateOnly(2026, 5, 5),
            MatrizBalanceada());

    private static DeclaracaoFiscal MscTransmitida()
    {
        var declaracao = NovaMsc();
        declaracao.TransmitirSiconfi(new DateOnly(2026, 6, 20), "PROTO-MSC-1");
        return declaracao;
    }

    // ---------- Invariantes ----------

    [Fact] // I-4 + Cenario 1: a declaracao nasce em Consolidada.
    public void Invariante_4_consolidacao_nasce_em_Consolidada()
    {
        var declaracao = NovaMsc();

        declaracao.Situacao.Should().Be(SituacaoDeclaracaoFiscal.Consolidada);
        declaracao.Competencia.Should().NotBeNull();
        declaracao.DataTransmissao.Should().BeNull();
    }

    [Fact] // I-2: exercicio inferior a 1900 e rejeitado.
    public void Invariante_2_exercicio_invalido_e_rejeitado()
    {
        var acao = () => DeclaracaoFiscal.ConsolidarMatriz(
            TenantA, TipoDeclaracaoFiscal.Dca, 1899, null, null, null,
            new DateOnly(2026, 1, 31), new DateOnly(2026, 1, 5), MatrizBalanceada());

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-3 + Cenario 7 + CB-6: Rreo informando competencia em vez de bimestre e rejeitado.
    public void Invariante_3_periodo_incompativel_com_tipo_e_rejeitado()
    {
        var acao = () => DeclaracaoFiscal.ConsolidarMatriz(
            TenantA, TipoDeclaracaoFiscal.Rreo, 2026, Competencia.De(2026, 5), null, null,
            new DateOnly(2026, 5, 30), new DateOnly(2026, 5, 5), MatrizBalanceada());

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-3: Msc sem competencia e rejeitado.
    public void Invariante_3_msc_sem_competencia_e_rejeitado()
    {
        var acao = () => DeclaracaoFiscal.ConsolidarMatriz(
            TenantA, TipoDeclaracaoFiscal.Msc, 2026, null, null, null,
            new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 5), MatrizBalanceada());

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-7 + Cenario 4: matriz desbalanceada e rejeitada na consolidacao.
    public void Invariante_7_matriz_desbalanceada_e_rejeitada()
    {
        var acao = () => DeclaracaoFiscal.ConsolidarMatriz(
            TenantA, TipoDeclaracaoFiscal.Msc, 2026, Competencia.De(2026, 5), null, null,
            new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 5), MatrizDesbalanceada());

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-7: matriz balanceada tem debitos == creditos.
    public void Invariante_7_matriz_balanceada_iguala_debitos_e_creditos()
    {
        var declaracao = NovaMsc();

        declaracao.Matriz.EstaBalanceada.Should().BeTrue();
        declaracao.Matriz.TotalDebitos.Valor.Should().Be(declaracao.Matriz.TotalCreditos.Valor);
    }

    [Fact] // I-5 + Cenario 3: transmissao fora de Consolidada e bloqueada.
    public void Invariante_5_transmissao_fora_de_Consolidada_e_bloqueada()
    {
        var declaracao = MscTransmitida();

        ((Action)(() => declaracao.TransmitirSiconfi(new DateOnly(2026, 6, 21), "PROTO-2")))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8 + CB-9: homologacao fora de Transmitida (sobre Consolidada) e bloqueada.
    public void Invariante_8_homologar_fora_de_Transmitida_e_bloqueado()
    {
        var declaracao = NovaMsc();

        ((Action)declaracao.Homologar).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-9: rejeicao fora de Transmitida (sobre Consolidada) e bloqueada.
    public void Invariante_9_rejeitar_fora_de_Transmitida_e_bloqueado()
    {
        var declaracao = NovaMsc();

        ((Action)(() => declaracao.Rejeitar("motivo"))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-10 + CB-10: estados terminais nao admitem novas transicoes.
    public void Invariante_10_estado_terminal_homologada_nao_admite_transicoes()
    {
        var declaracao = MscTransmitida();
        declaracao.Homologar();

        declaracao.Situacao.Should().Be(SituacaoDeclaracaoFiscal.Homologada);
        ((Action)(() => declaracao.TransmitirSiconfi(new DateOnly(2026, 6, 22), "P"))).Should().Throw<InvalidOperationException>();
        ((Action)declaracao.Homologar).Should().Throw<InvalidOperationException>();
        ((Action)(() => declaracao.Rejeitar("X"))).Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Consolidada --TransmitirSiconfi(Msc)--> Transmitida, emite MscEnviadaSiconfi.
    public void Transicao_transmitir_msc_de_Consolidada_para_Transmitida()
    {
        var declaracao = NovaMsc();

        declaracao.TransmitirSiconfi(new DateOnly(2026, 6, 20), "PROTO-MSC-1");

        declaracao.Situacao.Should().Be(SituacaoDeclaracaoFiscal.Transmitida);
        declaracao.DataTransmissao.Should().Be(new DateOnly(2026, 6, 20));
        declaracao.ProtocoloSiconfi.Should().Be("PROTO-MSC-1");
        declaracao.DomainEvents.OfType<MscEnviadaSiconfi>().Should().ContainSingle();
        declaracao.DomainEvents.OfType<DeclaracaoTransmitida>().Should().BeEmpty();
    }

    [Fact] // CB-8: Consolidada --TransmitirSiconfi(Rreo)--> Transmitida, emite DeclaracaoTransmitida.
    public void Transicao_transmitir_rreo_emite_DeclaracaoTransmitida()
    {
        var declaracao = NovoRreo();

        declaracao.TransmitirSiconfi(new DateOnly(2026, 5, 20), "PROTO-RREO-1");

        declaracao.Situacao.Should().Be(SituacaoDeclaracaoFiscal.Transmitida);
        declaracao.DomainEvents.OfType<DeclaracaoTransmitida>().Should().ContainSingle();
        declaracao.DomainEvents.OfType<MscEnviadaSiconfi>().Should().BeEmpty();
    }

    [Fact] // I-5: transmissao com protocolo vazio e rejeitada.
    public void Transicao_transmitir_com_protocolo_vazio_e_rejeitada()
    {
        var declaracao = NovaMsc();

        ((Action)(() => declaracao.TransmitirSiconfi(new DateOnly(2026, 6, 20), "  ")))
            .Should().Throw<ArgumentException>();
        declaracao.Situacao.Should().Be(SituacaoDeclaracaoFiscal.Consolidada);
    }

    [Fact] // I-8 + Cenario 5: Transmitida --Homologar--> Homologada, emite DeclaracaoHomologada.
    public void Transicao_homologar_de_Transmitida_para_Homologada()
    {
        var declaracao = MscTransmitida();

        declaracao.Homologar();

        declaracao.Situacao.Should().Be(SituacaoDeclaracaoFiscal.Homologada);
        declaracao.DomainEvents.OfType<DeclaracaoHomologada>().Should().ContainSingle();
    }

    [Fact] // I-9 + Cenario 6: Transmitida --Rejeitar--> Rejeitada, motivo registrado.
    public void Transicao_rejeitar_de_Transmitida_para_Rejeitada()
    {
        var declaracao = MscTransmitida();

        declaracao.Rejeitar("inconsistencia X");

        declaracao.Situacao.Should().Be(SituacaoDeclaracaoFiscal.Rejeitada);
        declaracao.MotivoRejeicao.Should().Be("inconsistencia X");
    }

    [Fact] // I-9: rejeicao com motivo vazio e rejeitada.
    public void Transicao_rejeitar_com_motivo_vazio_e_rejeitada()
    {
        var declaracao = MscTransmitida();

        ((Action)(() => declaracao.Rejeitar("   "))).Should().Throw<ArgumentException>();
        declaracao.Situacao.Should().Be(SituacaoDeclaracaoFiscal.Transmitida);
    }

    // ---------- Casos de borda dos Value Objects de periodo ----------

    [Fact] // CB-1: Competencia fora de 1..12 e rejeitada.
    public void Borda_competencia_fora_da_faixa_e_rejeitada()
        => ((Action)(() => Competencia.De(2026, 13))).Should().Throw<ArgumentOutOfRangeException>();

    [Fact] // CB-2: Bimestre fora de 1..6 e rejeitado.
    public void Borda_bimestre_fora_da_faixa_e_rejeitado()
        => ((Action)(() => Bimestre.De(2026, 7))).Should().Throw<ArgumentOutOfRangeException>();

    [Fact] // CB-3: Quadrimestre fora de 1..3 e rejeitado.
    public void Borda_quadrimestre_fora_da_faixa_e_rejeitado()
        => ((Action)(() => Quadrimestre.De(2026, 4))).Should().Throw<ArgumentOutOfRangeException>();

    [Fact] // CB-15: arredondamento contabil AwayFromZero.
    public void Borda_valor_monetario_arredonda_away_from_zero()
        => ValorMonetario.De(1.005m).Valor.Should().Be(1.01m);

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 2: o fluxo Consolidada->Transmitida->Homologada persiste e registra auditoria.
    public async Task Cenario_2_fluxo_de_transmissao_persiste_com_auditoria()
    {
        DeclaracaoFiscalId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var declaracao = NovaMsc();
            id = declaracao.Id;
            contexto.DeclaracoesFiscais.Add(declaracao);
            await contexto.SaveChangesAsync();

            declaracao.TransmitirSiconfi(new DateOnly(2026, 6, 20), "PROTO-MSC-1");
            await contexto.SaveChangesAsync();

            declaracao.Homologar();
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var declaracao = await contexto.DeclaracoesFiscais.SingleAsync(d => d.Id == id);
            declaracao.Situacao.Should().Be(SituacaoDeclaracaoFiscal.Homologada);
            declaracao.TenantId.Should().Be(TenantA);
            declaracao.ProtocoloSiconfi.Should().Be("PROTO-MSC-1");

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes da declaracao");
        }
    }

    [Fact] // Cenario 9 + CB-14: consulta e isolada por tenant (Global Query Filter).
    public async Task Cenario_9_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.DeclaracoesFiscais.Add(NovaMsc());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.DeclaracoesFiscais.ToListAsync()).Should().BeEmpty();
        }
    }
}
