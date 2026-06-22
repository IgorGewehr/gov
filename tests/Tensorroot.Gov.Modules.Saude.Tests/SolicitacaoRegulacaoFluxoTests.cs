using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="SolicitacaoRegulacao"/>: invariantes, cada
/// transicao da maquina de estados e os cenarios BDD de SolicitacaoRegulacao.rules.md (cota,
/// reserva SISREG, fila), sobre SQLite em memoria com auditoria e isolamento por tenant.
/// </summary>
public sealed class SolicitacaoRegulacaoFluxoTests : SaudeTestBase
{
    private static readonly DateOnly DataSolicitacao = new(2026, 6, 21);

    private static Procedimento NovoProcedimento()
        => Procedimento.Criar("0301010010", "Consulta medica em atencao especializada");

    private static SolicitacaoRegulacao NovaSolicitacao(int cotaDisponivel = 5, Prioridade prioridade = Prioridade.Eletiva)
        => SolicitacaoRegulacao.Solicitar(
            TenantA,
            PacienteId.New(),
            EstabelecimentoId.New(),
            ProfissionalId.New(),
            NovoProcedimento(),
            prioridade,
            Cota.Criar(cotaDisponivel, 10),
            "Encaminhamento por suspeita clinica",
            DataSolicitacao);

    private static SolicitacaoRegulacao SolicitacaoAutorizada(int cotaDisponivel = 5)
    {
        var solicitacao = NovaSolicitacao(cotaDisponivel);
        solicitacao.Autorizar("SISREG-001", DataSolicitacao);
        return solicitacao;
    }

    // ---------- Invariantes ----------

    [Fact] // I-10: criacao nasce Solicitada e emite RegulacaoSolicitada.
    public void Invariante_10_nasce_solicitada_e_emite_evento()
    {
        var solicitacao = NovaSolicitacao();

        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Solicitada);
        solicitacao.DomainEvents.OfType<RegulacaoSolicitada>().Should().ContainSingle();
    }

    [Fact] // I-1 + B-1: justificativa vazia e rejeitada.
    public void Invariante_1_justificativa_vazia_e_rejeitada()
    {
        var acao = () => SolicitacaoRegulacao.Solicitar(
            TenantA, PacienteId.New(), EstabelecimentoId.New(), ProfissionalId.New(),
            NovoProcedimento(), Prioridade.Eletiva, Cota.Criar(1, 1), "   ", DataSolicitacao);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-1 + B-1: procedimento SIGTAP sem codigo e rejeitado.
    public void Invariante_1_procedimento_sem_codigo_e_rejeitado()
    {
        var acao = () => Procedimento.Criar("  ", "Descricao");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-11 + B-12: prioridade invalida e rejeitada.
    public void Invariante_11_prioridade_invalida_e_rejeitada()
    {
        var acao = () => SolicitacaoRegulacao.Solicitar(
            TenantA, PacienteId.New(), EstabelecimentoId.New(), ProfissionalId.New(),
            NovoProcedimento(), (Prioridade)99, Cota.Criar(1, 1), "Justificativa", DataSolicitacao);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-3 + Cenario 2 + B-3: autorizar sem cota e rejeitado; situacao inalterada.
    public void Invariante_3_autorizar_sem_cota_e_rejeitado()
    {
        var solicitacao = NovaSolicitacao(cotaDisponivel: 0);

        var acao = () => solicitacao.Autorizar("SISREG-X", DataSolicitacao);

        acao.Should().Throw<InvalidOperationException>();
        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Solicitada);
    }

    [Fact] // I-4: autorizar sem protocolo SISREG e rejeitado.
    public void Invariante_4_autorizar_sem_protocolo_e_rejeitado()
    {
        var solicitacao = NovaSolicitacao();

        var acao = () => solicitacao.Autorizar("   ", DataSolicitacao);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-6 + B-5: devolver solicitacao Autorizada e rejeitado (so de Solicitada).
    public void Invariante_6_devolver_autorizada_e_rejeitado()
    {
        var solicitacao = SolicitacaoAutorizada();

        var acao = () => solicitacao.Devolver("Anexar exame");

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8 + B-6: executar solicitacao nao Autorizada e rejeitado.
    public void Invariante_8_executar_nao_autorizada_e_rejeitado()
    {
        var solicitacao = NovaSolicitacao();

        var acao = () => solicitacao.Executar(DataSolicitacao);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-9 + Cenario 8 + B-7: situacao encerrada nao admite novas transicoes.
    public void Invariante_9_situacao_encerrada_nao_admite_transicoes()
    {
        var solicitacao = SolicitacaoAutorizada();
        solicitacao.Executar(DataSolicitacao);

        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Executada);
        ((Action)(() => solicitacao.Autorizar("SISREG-Y", DataSolicitacao))).Should().Throw<InvalidOperationException>();
        ((Action)(() => solicitacao.Negar("tarde"))).Should().Throw<InvalidOperationException>();
        ((Action)(() => solicitacao.Cancelar("tarde"))).Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // I-3/I-4 + Cenario 1: Solicitada --Autorizar--> Autorizada, consome cota, emite SolicitacaoAutorizada.
    public void Transicao_autorizar_de_solicitada_para_autorizada()
    {
        var solicitacao = NovaSolicitacao(cotaDisponivel: 5);

        solicitacao.Autorizar("SISREG-123", DataSolicitacao);

        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Autorizada);
        solicitacao.Cota.Disponivel.Should().Be(4);
        solicitacao.ProtocoloSisreg.Should().Be("SISREG-123");
        solicitacao.DataAutorizacao.Should().Be(DataSolicitacao);
        solicitacao.DomainEvents.OfType<SolicitacaoAutorizada>().Should().ContainSingle();
    }

    [Fact] // I-5 + Cenario 4: Solicitada --Negar--> Negada, emite SolicitacaoNegada.
    public void Transicao_negar_de_solicitada_para_negada()
    {
        var solicitacao = NovaSolicitacao();

        solicitacao.Negar("Sem indicacao clinica");

        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Negada);
        solicitacao.DomainEvents.OfType<SolicitacaoNegada>().Should().ContainSingle();
    }

    [Fact] // I-6/I-3 + Cenario 5: Solicitada --Devolver--> Devolvida --Autorizar--> Autorizada.
    public void Transicao_devolver_e_reautorizar()
    {
        var solicitacao = NovaSolicitacao();

        solicitacao.Devolver("Anexar exame");
        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Devolvida);
        solicitacao.DomainEvents.OfType<SolicitacaoDevolvida>().Should().ContainSingle();

        solicitacao.Autorizar("SISREG-456", DataSolicitacao);
        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Autorizada);
    }

    [Fact] // I-8 + Cenario 6: Autorizada --Executar--> Executada, emite ProcedimentoExecutado.
    public void Transicao_executar_de_autorizada_para_executada()
    {
        var solicitacao = SolicitacaoAutorizada();

        solicitacao.Executar(DataSolicitacao);

        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Executada);
        solicitacao.DomainEvents.OfType<ProcedimentoExecutado>().Should().ContainSingle();
    }

    [Fact] // I-7 + Cenario 7 + B-9: cancelar Autorizada devolve a cota e emite SolicitacaoCancelada.
    public void Transicao_cancelar_autorizada_devolve_cota()
    {
        var solicitacao = NovaSolicitacao(cotaDisponivel: 5);
        solicitacao.Autorizar("SISREG-789", DataSolicitacao);
        solicitacao.Cota.Disponivel.Should().Be(4);

        solicitacao.Cancelar("Paciente desistiu");

        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Cancelada);
        solicitacao.Cota.Disponivel.Should().Be(5, "a cota reservada e devolvida ao cancelar uma autorizada");
        solicitacao.DomainEvents.OfType<SolicitacaoCancelada>().Should().ContainSingle();
    }

    [Fact] // I-7 + B-8: cancelar Solicitada (sem reserva) nao mexe na cota.
    public void Transicao_cancelar_solicitada_nao_devolve_cota()
    {
        var solicitacao = NovaSolicitacao(cotaDisponivel: 5);

        solicitacao.Cancelar("Desistencia");

        solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Cancelada);
        solicitacao.Cota.Disponivel.Should().Be(5);
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 1: autorizacao persiste com cota consumida, protocolo e auditoria.
    public async Task Cenario_1_autorizacao_persiste_com_auditoria()
    {
        SolicitacaoRegulacaoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var solicitacao = NovaSolicitacao(cotaDisponivel: 3);
            id = solicitacao.Id;
            contexto.SolicitacoesRegulacao.Add(solicitacao);
            await contexto.SaveChangesAsync();

            solicitacao.Autorizar("SISREG-PERSIST", DataSolicitacao);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var solicitacao = await contexto.SolicitacoesRegulacao.SingleAsync(s => s.Id == id);
            solicitacao.Situacao.Should().Be(SituacaoSolicitacaoRegulacao.Autorizada);
            solicitacao.Cota.Disponivel.Should().Be(2);
            solicitacao.ProtocoloSisreg.Should().Be("SISREG-PERSIST");
            solicitacao.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes da solicitacao");
        }
    }

    [Fact] // Cenario 10: fila de regulacao e tenant-scoped (Global Query Filter).
    public async Task Cenario_10_fila_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.SolicitacoesRegulacao.Add(NovaSolicitacao(prioridade: Prioridade.Urgente));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.SolicitacoesRegulacao.ToListAsync()).Should().BeEmpty();
        }
    }
}
