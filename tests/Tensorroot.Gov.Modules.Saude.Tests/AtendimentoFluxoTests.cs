using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.Modules.Saude.Domain.Events;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Atendimento"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Atendimento.rules.md (append-only, NGS2/ICP-Brasil,
/// RNDS, SISAB), sobre SQLite em memoria com auditoria e isolamento por tenant.
/// </summary>
public sealed class AtendimentoFluxoTests : SaudeTestBase
{
    private static readonly DateTimeOffset DataHora = new(2026, 6, 21, 9, 0, 0, TimeSpan.Zero);

    private static Atendimento NovoAtendimento(ModalidadeAtendimento modalidade = ModalidadeAtendimento.Presencial)
        => Atendimento.Registrar(
            TenantA,
            PacienteId.New(),
            EstabelecimentoId.New(),
            ProfissionalId.New(),
            DataHora,
            Competencia.De(DataHora),
            modalidade);

    private static AssinaturaDigital AssinaturaNgs2()
        => AssinaturaDigital.De("ICP-BR-CERT-001", "HASH-ABC", DataHora, NivelGarantia.NGS2);

    private static AssinaturaDigital AssinaturaNgs1()
        => AssinaturaDigital.De("ICP-BR-CERT-002", "HASH-DEF", DataHora, NivelGarantia.NGS1);

    private static Atendimento AtendimentoComEvolucao()
    {
        var atendimento = NovoAtendimento();
        atendimento.AdicionarEvolucaoSOAP("Cefaleia", "PA 120x80", "Cefaleia tensional", "Analgesico", DataHora);
        return atendimento;
    }

    private static Atendimento AtendimentoAssinado()
    {
        var atendimento = AtendimentoComEvolucao();
        atendimento.Assinar(AssinaturaNgs2());
        return atendimento;
    }

    // ---------- Invariantes ----------

    [Fact] // I-11: criacao nasce EmAndamento e emite AtendimentoRegistrado.
    public void Invariante_11_registro_nasce_em_andamento_e_emite_evento()
    {
        var atendimento = NovoAtendimento();

        atendimento.Situacao.Should().Be(SituacaoAtendimento.EmAndamento);
        atendimento.NivelGarantia.Should().Be(NivelGarantia.NGS1);
        atendimento.DomainEvents.OfType<AtendimentoRegistrado>().Should().ContainSingle();
    }

    [Fact] // I-1: referencia de paciente vazia e rejeitada.
    public void Invariante_1_paciente_obrigatorio()
    {
        var acao = () => Atendimento.Registrar(
            TenantA, new PacienteId(Guid.Empty), EstabelecimentoId.New(), ProfissionalId.New(),
            DataHora, Competencia.De(DataHora), ModalidadeAtendimento.Presencial);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-8 + B-3: evolucao com todos os campos SOAP vazios e rejeitada.
    public void Invariante_8_evolucao_soap_totalmente_vazia_e_rejeitada()
    {
        var atendimento = NovoAtendimento();

        var acao = () => atendimento.AdicionarEvolucaoSOAP(null, null, null, null, DataHora);

        acao.Should().Throw<ArgumentException>();
        atendimento.Evolucoes.Should().BeEmpty();
    }

    [Fact] // I-3 + Cenario 5 + B-4: assinar sem evolucao e rejeitado.
    public void Invariante_3_assinar_sem_evolucao_e_rejeitado()
    {
        var atendimento = NovoAtendimento();

        var acao = () => atendimento.Assinar(AssinaturaNgs2());

        acao.Should().Throw<InvalidOperationException>();
        atendimento.Situacao.Should().Be(SituacaoAtendimento.EmAndamento);
    }

    [Fact] // I-3/I-4: assinatura NGS1 e rejeitada (exige NGS2).
    public void Invariante_4_assinar_exige_ngs2()
    {
        var atendimento = AtendimentoComEvolucao();

        var acao = () => atendimento.Assinar(AssinaturaNgs1());

        acao.Should().Throw<InvalidOperationException>();
        atendimento.Situacao.Should().Be(SituacaoAtendimento.EmAndamento);
    }

    [Fact] // I-2/I-7 + Cenario 2 + B-5/B-7: assinado nao admite evolucao/prescricao/exame diretos.
    public void Invariante_2_assinado_e_imutavel_para_inclusoes_diretas()
    {
        var atendimento = AtendimentoAssinado();

        ((Action)(() => atendimento.AdicionarEvolucaoSOAP("X", null, null, null, DataHora)))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => atendimento.AdicionarPrescricao("Dipirona", "1cp 8/8h", DataHora)))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => atendimento.AdicionarSolicitacaoExame("Hemograma", "Rotina", DataHora)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-2 + B-6: adendo referenciando evolucao inexistente/nao assinada e rejeitado.
    public void Invariante_2_adendo_exige_evolucao_assinada_existente()
    {
        var atendimento = AtendimentoAssinado();

        var acao = () => atendimento.AdicionarAdendo(Guid.NewGuid(), "Correcao", AssinaturaNgs2(), DataHora);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-5/I-4 + Cenario 9 + B-9: compartilhar na RNDS exige Assinado em NGS2.
    public void Invariante_5_compartilhar_exige_assinado()
    {
        var atendimento = AtendimentoComEvolucao();

        var acao = () => atendimento.CompartilharNaRNDS("PROTO-RNDS-1");

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-6 + B-11: lancar no SISAB exige Assinado/Compartilhado.
    public void Invariante_6_lancar_sisab_exige_assinado()
    {
        var atendimento = AtendimentoComEvolucao();

        var acao = () => atendimento.LancarNoSISAB(Competencia.De(DataHora));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-10 + Cenario 7 + B-13: cancelar atendimento assinado e rejeitado.
    public void Invariante_10_cancelar_assinado_e_rejeitado()
    {
        var atendimento = AtendimentoAssinado();

        var acao = () => atendimento.Cancelar("Erro de digitacao");

        acao.Should().Throw<InvalidOperationException>();
        atendimento.Situacao.Should().Be(SituacaoAtendimento.Assinado);
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // EmAndamento --AdicionarEvolucaoSOAP--> EmAndamento; define CID quando informado.
    public void Transicao_adicionar_evolucao_mantem_em_andamento()
    {
        var atendimento = NovoAtendimento();

        atendimento.AdicionarEvolucaoSOAP("S", "O", "A", "P", DataHora, Cid.De("J45"));

        atendimento.Situacao.Should().Be(SituacaoAtendimento.EmAndamento);
        atendimento.Evolucoes.Should().ContainSingle().Which.Assinada.Should().BeFalse();
        atendimento.Cid!.Value.Codigo.Should().Be("J45");
    }

    [Fact] // I-3 + Cenario 5: EmAndamento --Assinar--> Assinado (NGS2), evolucoes marcadas assinadas.
    public void Transicao_assinar_de_em_andamento_para_assinado()
    {
        var atendimento = AtendimentoComEvolucao();

        atendimento.Assinar(AssinaturaNgs2());

        atendimento.Situacao.Should().Be(SituacaoAtendimento.Assinado);
        atendimento.NivelGarantia.Should().Be(NivelGarantia.NGS2);
        atendimento.Evolucoes.Should().OnlyContain(e => e.Assinada);
        atendimento.DomainEvents.OfType<AtendimentoAssinado>().Should().ContainSingle();
    }

    [Fact] // Cenario 6 + I-2: Assinado --AdicionarAdendo--> mantem; original inalterada, emite AdendoRegistrado.
    public void Transicao_adendo_apos_assinatura_mantem_original_inalterada()
    {
        var atendimento = AtendimentoAssinado();
        var original = atendimento.Evolucoes.Single();
        var textoOriginal = original.Plano;

        atendimento.AdicionarAdendo(original.Id.Value, "Correcao: ajuste de dose", AssinaturaNgs2(), DataHora);

        atendimento.Situacao.Should().Be(SituacaoAtendimento.Assinado);
        atendimento.Evolucoes.Should().HaveCount(2);
        atendimento.Evolucoes.Single(e => e.EhAdendo).EvolucaoReferenciadaId.Should().Be(original.Id.Value);
        atendimento.Evolucoes.Single(e => e.Id == original.Id).Plano.Should().Be(textoOriginal);
        atendimento.DomainEvents.OfType<AdendoRegistrado>().Should().ContainSingle();
    }

    [Fact] // I-5 + Cenario 3: Assinado --CompartilharNaRNDS--> Compartilhado, emite RESCompartilhadoNaRNDS.
    public void Transicao_compartilhar_de_assinado_para_compartilhado()
    {
        var atendimento = AtendimentoAssinado();

        atendimento.CompartilharNaRNDS("PROTO-RNDS-001");

        atendimento.Situacao.Should().Be(SituacaoAtendimento.Compartilhado);
        atendimento.ProtocoloRnds.Should().Be("PROTO-RNDS-001");
        atendimento.DomainEvents.OfType<RESCompartilhadoNaRNDS>().Should().ContainSingle();
    }

    [Fact] // I-6 + Cenario 4: Assinado --LancarNoSISAB--> mantem, emite PEPLancadoNoSISAB.
    public void Transicao_lancar_sisab_mantem_situacao_e_emite_evento()
    {
        var atendimento = AtendimentoAssinado();

        atendimento.LancarNoSISAB(Competencia.De(DataHora));

        atendimento.Situacao.Should().Be(SituacaoAtendimento.Assinado);
        atendimento.DomainEvents.OfType<PEPLancadoNoSISAB>().Should().ContainSingle();
    }

    [Fact] // I-10 + Cenario 7: EmAndamento --Cancelar--> Cancelado, emite AtendimentoCancelado.
    public void Transicao_cancelar_de_em_andamento_para_cancelado()
    {
        var atendimento = NovoAtendimento();

        atendimento.Cancelar("Erro de digitacao");

        atendimento.Situacao.Should().Be(SituacaoAtendimento.Cancelado);
        atendimento.DomainEvents.OfType<AtendimentoCancelado>().Should().ContainSingle()
            .Which.Motivo.Should().Be("Erro de digitacao");
    }

    [Fact] // Compartilhado ainda admite LancarNoSISAB (I-12) mas nao cancelamento.
    public void Transicao_compartilhado_admite_sisab_mas_nao_cancelamento()
    {
        var atendimento = AtendimentoAssinado();
        atendimento.CompartilharNaRNDS("PROTO-RNDS-002");

        atendimento.LancarNoSISAB(Competencia.De(DataHora));
        atendimento.DomainEvents.OfType<PEPLancadoNoSISAB>().Should().ContainSingle();

        ((Action)(() => atendimento.Cancelar("tarde demais"))).Should().Throw<InvalidOperationException>();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Persistencia: fluxo de assinatura persiste com auditoria e mantem append-only.
    public async Task Fluxo_de_assinatura_persiste_com_auditoria()
    {
        AtendimentoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var atendimento = AtendimentoComEvolucao();
            id = atendimento.Id;
            contexto.Atendimentos.Add(atendimento);
            await contexto.SaveChangesAsync();

            atendimento.Assinar(AssinaturaNgs2());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var atendimento = await contexto.Atendimentos
                .Include(a => a.Evolucoes)
                .SingleAsync(a => a.Id == id);
            atendimento.Situacao.Should().Be(SituacaoAtendimento.Assinado);
            atendimento.TenantId.Should().Be(TenantA);
            atendimento.Evolucoes.Should().OnlyContain(e => e.Assinada);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes do atendimento");
        }
    }

    [Fact] // Cenario 10: consulta do prontuario e tenant-scoped (Global Query Filter).
    public async Task Cenario_10_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Atendimentos.Add(AtendimentoComEvolucao());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Atendimentos.ToListAsync()).Should().BeEmpty();
        }
    }
}
