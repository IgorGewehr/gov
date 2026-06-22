using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Transparencia.Domain.Events;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="RemessaTce"/>: invariantes, cada transicao da
/// maquina de estados, os cenarios BDD e casos de borda de RemessaTce.rules.md e o isolamento por
/// tenant — sobre SQLite em memoria com auditoria, Outbox e Global Query Filter.
/// </summary>
public sealed class RemessaTceFluxoTests : TransparenciaTestBase
{
    private static readonly byte[] Pacote = Encoding.UTF8.GetBytes("pacote-siapc-2026-B03");

    private static RemessaTce NovaRemessa(
        Guid? tenant = null,
        int exercicio = 2026,
        TipoPeriodo tipo = TipoPeriodo.Bimestre,
        int numero = 3,
        DateOnly? dataLimite = null,
        byte[]? conteudo = null)
    {
        var bytes = conteudo ?? Pacote;
        var arquivo = ArquivoRemessa.Criar(
            "balancete.xml",
            bytes,
            [RegistroLeiaute.Criar("00", "cabecalho")]);

        return RemessaTce.GerarRemessa(
            tenant ?? TenantA,
            Periodo.De(exercicio, tipo, numero),
            Leiaute.De("SIAPC", "2026.1"),
            dataLimite ?? new DateOnly(2026, 8, 31),
            new DateOnly(2026, 7, 1),
            [arquivo],
            bytes);
    }

    private static ResultadoValidacao RdiLimpo()
        => ResultadoValidacao.Criar("2026.1", DateTimeOffset.UtcNow, []);

    private static ResultadoValidacao RdiComErro()
        => ResultadoValidacao.Criar(
            "2026.1",
            DateTimeOffset.UtcNow,
            [OcorrenciaValidacao.Criar("balancete.xml", 10, SeveridadeOcorrencia.Erro, "Conta invalida")]);

    private static ResultadoValidacao RdiSoAvisos()
        => ResultadoValidacao.Criar(
            "2026.1",
            DateTimeOffset.UtcNow,
            [OcorrenciaValidacao.Criar("balancete.xml", 5, SeveridadeOcorrencia.Aviso, "Atributo opcional ausente")]);

    private static RemessaTce RemessaValidada()
    {
        var remessa = NovaRemessa();
        remessa.RegistrarResultadoValidacao(RdiLimpo());
        return remessa;
    }

    private const string ZipPadrao = "99999999000199.01032026.30042026.01072026.P.000000202603.zip";

    // Helper: leva a remessa de Validada a Enviada pelo fluxo correto (empacotar + registrar protocolo).
    private static RemessaTce RemessaEnviada(DateOnly? dataRecibo = null)
    {
        var remessa = RemessaValidada();
        remessa.MarcarProntaParaTransmissao(ZipPadrao, Pacote);
        remessa.RegistrarProtocolo("PROTO-TCE-001", dataRecibo ?? new DateOnly(2026, 8, 1));
        return remessa;
    }

    // ---------- Invariantes ----------

    [Fact] // I-3 + Cenario 1: a remessa nasce em Gerada, com hash, e emite RemessaGerada.
    public void Invariante_3_geracao_nasce_em_Gerada_com_hash_e_emite_evento()
    {
        var remessa = NovaRemessa();

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Gerada);
        remessa.HashIntegridade.Should().NotBeNull();
        remessa.DataEnvio.Should().BeNull();
        remessa.DomainEvents.OfType<RemessaGerada>().Should().ContainSingle();
    }

    [Fact] // I-2: geracao sem nenhum arquivo e rejeitada.
    public void Invariante_2_geracao_sem_arquivos_e_rejeitada()
    {
        var acao = () => RemessaTce.GerarRemessa(
            TenantA,
            Periodo.De(2026, TipoPeriodo.Bimestre, 3),
            Leiaute.De("SIAPC", "2026.1"),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 7, 1),
            [],
            Pacote);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // CB-1: Periodo bimestre fora de 1..6 e rejeitado no VO.
    public void Invariante_2_periodo_bimestre_fora_da_faixa_e_rejeitado()
    {
        var acao = () => Periodo.De(2026, TipoPeriodo.Bimestre, 7);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // CB-2: Periodo mensal fora de 1..12 e rejeitado no VO.
    public void Invariante_2_periodo_mensal_fora_da_faixa_e_rejeitado()
    {
        var acao = () => Periodo.De(2026, TipoPeriodo.Mensal, 13);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // CB-3: Leiaute com codigo/versao vazios e rejeitado no VO.
    public void Invariante_2_leiaute_vazio_e_rejeitado()
    {
        ((Action)(() => Leiaute.De("", "2026.1"))).Should().Throw<ArgumentException>();
        ((Action)(() => Leiaute.De("SIAPC", "  "))).Should().Throw<ArgumentException>();
    }

    [Fact] // I-5 + Cenario 2: validacao com erro transita para Rejeitada e bloqueia o envio.
    public void Invariante_5_validacao_com_erro_rejeita_e_bloqueia_envio()
    {
        var remessa = NovaRemessa();

        remessa.RegistrarResultadoValidacao(RdiComErro());

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Rejeitada);
        remessa.ResultadoValidacao!.PossuiErro.Should().BeTrue();
        remessa.DomainEvents.OfType<RemessaRejeitada>().Should().ContainSingle()
            .Which.QuantidadeErros.Should().Be(1);

        ((Action)(() => remessa.MarcarProntaParaTransmissao(ZipPadrao, Pacote)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-6 + CB-6: RDI sem erro (ainda que com avisos) transita para Validada.
    public void Invariante_6_validacao_sem_erro_com_avisos_transita_para_Validada()
    {
        var remessa = NovaRemessa();

        remessa.RegistrarResultadoValidacao(RdiSoAvisos());

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Validada);
        remessa.ResultadoValidacao!.PossuiErro.Should().BeFalse();
        remessa.ResultadoValidacao.QuantidadeAvisos.Should().Be(1);
        remessa.DomainEvents.OfType<RemessaValidada>().Should().ContainSingle();
    }

    [Fact] // I-4 + CB-5: validar fora de Gerada e bloqueado.
    public void Invariante_4_validar_fora_de_Gerada_e_bloqueado()
    {
        var remessa = RemessaValidada();

        ((Action)(() => remessa.RegistrarResultadoValidacao(RdiLimpo())))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-7 + Cenario 7: empacotamento com hash divergente do conteudo e bloqueado.
    public void Invariante_7_empacotamento_com_hash_divergente_e_bloqueado()
    {
        var remessa = RemessaValidada();

        var acao = () => remessa.MarcarProntaParaTransmissao(ZipPadrao, Encoding.UTF8.GetBytes("conteudo-adulterado"));

        acao.Should().Throw<InvalidOperationException>();
        remessa.Situacao.Should().Be(SituacaoRemessaTce.Validada);
    }

    [Fact] // I-7 + Cenario 4: empacotamento a partir de Gerada (sem validacao limpa) e bloqueado.
    public void Invariante_7_empacotamento_sem_validacao_limpa_e_bloqueado()
    {
        var remessa = NovaRemessa();

        ((Action)(() => remessa.MarcarProntaParaTransmissao(ZipPadrao, Pacote)))
            .Should().Throw<InvalidOperationException>();
        remessa.Situacao.Should().Be(SituacaoRemessaTce.Gerada);
    }

    [Fact] // I-10: estados terminais (Rejeitada) nao admitem novas transicoes.
    public void Invariante_10_estado_terminal_rejeitada_nao_admite_transicoes()
    {
        var remessa = NovaRemessa();
        remessa.RegistrarResultadoValidacao(RdiComErro());

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Rejeitada);
        ((Action)(() => remessa.RegistrarResultadoValidacao(RdiLimpo()))).Should().Throw<InvalidOperationException>();
        ((Action)(() => remessa.MarcarProntaParaTransmissao(ZipPadrao, Pacote))).Should().Throw<InvalidOperationException>();
        ((Action)remessa.Homologar).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-10 + CB-8: homologar fora de Enviada (sobre Validada) e bloqueado.
    public void Invariante_10_homologar_fora_de_Enviada_e_bloqueado()
    {
        var remessa = RemessaValidada();

        ((Action)remessa.Homologar).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-12 + CB-9: VencerPrazo com hoje == DataLimite nao dispara.
    public void Invariante_12_vencer_prazo_na_data_limite_nao_dispara()
    {
        var remessa = NovaRemessa(dataLimite: new DateOnly(2026, 8, 31));

        ((Action)(() => remessa.VencerPrazo(new DateOnly(2026, 8, 31))))
            .Should().Throw<InvalidOperationException>();
        remessa.AlertaPrazoEmitido.Should().BeFalse();
    }

    [Fact] // I-12 + Cenario 6: VencerPrazo apos a data emite PrazoRemessaVencido sem mudar a situacao.
    public void Invariante_12_vencer_prazo_apos_data_emite_alerta()
    {
        var remessa = NovaRemessa(dataLimite: new DateOnly(2026, 8, 31));

        remessa.VencerPrazo(new DateOnly(2026, 9, 1));

        remessa.AlertaPrazoEmitido.Should().BeTrue();
        remessa.Situacao.Should().Be(SituacaoRemessaTce.Gerada);
        remessa.DomainEvents.OfType<PrazoRemessaVencido>().Should().ContainSingle()
            .Which.DataLimite.Should().Be(new DateOnly(2026, 8, 31));
    }

    [Fact] // I-12 + CB-10: VencerPrazo e idempotente — emite o alerta uma unica vez.
    public void Invariante_12_vencer_prazo_e_idempotente()
    {
        var remessa = NovaRemessa(dataLimite: new DateOnly(2026, 8, 31));
        remessa.VencerPrazo(new DateOnly(2026, 9, 1));

        ((Action)(() => remessa.VencerPrazo(new DateOnly(2026, 9, 2))))
            .Should().Throw<InvalidOperationException>();
        remessa.DomainEvents.OfType<PrazoRemessaVencido>().Should().ContainSingle();
    }

    [Fact] // I-12 + CB-11: VencerPrazo sobre remessa ja Enviada e bloqueado.
    public void Invariante_12_vencer_prazo_sobre_enviada_e_bloqueado()
    {
        var remessa = RemessaEnviada();

        ((Action)(() => remessa.VencerPrazo(new DateOnly(2026, 9, 1))))
            .Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Gerada --Validar(sem erro)--> Validada.
    public void Transicao_validar_de_Gerada_para_Validada()
    {
        var remessa = NovaRemessa();

        remessa.RegistrarResultadoValidacao(RdiLimpo());

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Validada);
        remessa.DomainEvents.OfType<RemessaValidada>().Should().ContainSingle();
    }

    [Fact] // Gerada --Validar(com erro)--> Rejeitada.
    public void Transicao_validar_de_Gerada_para_Rejeitada()
    {
        var remessa = NovaRemessa();

        remessa.RegistrarResultadoValidacao(RdiComErro());

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Rejeitada);
    }

    [Fact] // Validada --Empacotar--> ProntaParaTransmissao --RegistrarProtocolo--> Enviada.
    public void Transicao_empacotar_e_registrar_protocolo_de_Validada_para_Enviada()
    {
        var remessa = RemessaValidada();

        remessa.MarcarProntaParaTransmissao(ZipPadrao, Pacote);
        remessa.Situacao.Should().Be(SituacaoRemessaTce.ProntaParaTransmissao);
        remessa.NomeArquivoZip.Should().Be(ZipPadrao);
        remessa.DomainEvents.OfType<RemessaProntaParaTransmissao>().Should().ContainSingle();

        remessa.RegistrarProtocolo("PROTO-TCE-001", new DateOnly(2026, 8, 1));

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Enviada);
        remessa.DataEnvio.Should().Be(new DateOnly(2026, 8, 1));
        remessa.ProtocoloTce.Should().Be("PROTO-TCE-001");
        remessa.DomainEvents.OfType<RemessaEnviadaTce>().Should().ContainSingle()
            .Which.Protocolo.Should().Be("PROTO-TCE-001");
    }

    [Fact] // Enviada --Homologar--> Homologada, emite RemessaHomologada.
    public void Transicao_homologar_de_Enviada_para_Homologada()
    {
        var remessa = RemessaEnviada();

        remessa.Homologar();

        remessa.Situacao.Should().Be(SituacaoRemessaTce.Homologada);
        remessa.DomainEvents.OfType<RemessaHomologada>().Should().ContainSingle();
    }

    [Fact] // CB-15: round-trip de HashIntegridade.Confere retorna true para conteudo integro.
    public void Hash_integridade_confere_conteudo_integro()
    {
        var remessa = NovaRemessa();

        remessa.HashIntegridade!.Confere(Pacote).Should().BeTrue();
        remessa.HashIntegridade.Confere(Encoding.UTF8.GetBytes("outro")).Should().BeFalse();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 3: o fluxo Gerada->Validada->Enviada persiste e registra auditoria.
    public async Task Cenario_3_fluxo_de_envio_persiste_com_auditoria()
    {
        RemessaTceId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var remessa = NovaRemessa();
            id = remessa.Id;
            contexto.RemessasTce.Add(remessa);
            await contexto.SaveChangesAsync();

            remessa.RegistrarResultadoValidacao(RdiLimpo());
            await contexto.SaveChangesAsync();

            remessa.MarcarProntaParaTransmissao(ZipPadrao, Pacote);
            await contexto.SaveChangesAsync();

            remessa.RegistrarProtocolo("PROTO-TCE-001", new DateOnly(2026, 8, 1));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var remessa = await contexto.RemessasTce.SingleAsync(r => r.Id == id);
            remessa.Situacao.Should().Be(SituacaoRemessaTce.Enviada);
            remessa.TenantId.Should().Be(TenantA);
            remessa.DataEnvio.Should().Be(new DateOnly(2026, 8, 1));

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes da remessa");
        }
    }

    [Fact] // Cenario 8 + CB-14: consulta e isolada por tenant (Global Query Filter).
    public async Task Cenario_8_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.RemessasTce.Add(NovaRemessa());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.RemessasTce.ToListAsync()).Should().BeEmpty();
        }
    }
}
