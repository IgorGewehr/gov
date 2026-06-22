using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Protocolo.Domain.Events;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Protocolo.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Processo"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Processo.rules.md, sobre SQLite em memoria com
/// auditoria, Outbox e isolamento por tenant.
/// </summary>
public sealed class ProcessoFluxoTests : ProtocoloTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    private static Processo NovoProcesso(
        NivelDeAcesso nivelAcesso = NivelDeAcesso.Publico,
        string? origemModulo = null,
        Guid? origemId = null,
        string nup = "NUP-2026-0001",
        string classificacao = "025.2")
        => Processo.Autuar(
            TenantA,
            new Nup(nup),
            new Classificacao(classificacao),
            nivelAcesso,
            requerimentoId: Guid.NewGuid(),
            origemModulo,
            origemId,
            Hoje);

    // ---------- Invariantes ----------

    [Fact] // I-2 + Cenario 1: a autuacao nasce em Autuado, gera NUP e emite ProcessoAutuado.
    public void Invariante_2_autuacao_nasce_em_Autuado_e_emite_evento()
    {
        var processo = NovoProcesso();

        processo.Situacao.Should().Be(SituacaoProcesso.Autuado);
        processo.Nup.Valor.Should().Be("NUP-2026-0001");
        processo.DomainEvents.OfType<ProcessoAutuado>().Should().ContainSingle();
    }

    [Fact] // I-3 + B-1: NUP vazio na autuacao e rejeitado (o VO Nup ja valida).
    public void Invariante_3_nup_vazio_e_rejeitado()
    {
        var acao = () => new Nup("   ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-3 + B-1: classificacao vazia na autuacao e rejeitada.
    public void Invariante_3_classificacao_vazia_e_rejeitada()
    {
        var acao = () => new Classificacao("  ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-12 + B-10: origem obrigatoria quando ha modulo originador.
    public void Invariante_12_origem_obrigatoria_com_modulo_originador()
    {
        var acao = () => Processo.Autuar(
            TenantA, new Nup("NUP-X"), new Classificacao("025.2"), NivelDeAcesso.Publico,
            requerimentoId: null, origemModulo: "Licitacao", origemId: Guid.Empty, Hoje);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-12 + Cenario 2: autuado por outro modulo preserva OrigemModulo/OrigemId.
    public void Invariante_12_autuacao_por_outro_modulo_preserva_origem()
    {
        var origemId = Guid.NewGuid();
        var processo = NovoProcesso(origemModulo: "Licitacao", origemId: origemId);

        processo.OrigemModulo.Should().Be("Licitacao");
        processo.OrigemId.Should().Be(origemId);
        processo.DomainEvents.OfType<ProcessoAutuado>().Should().ContainSingle()
            .Which.OrigemModulo.Should().Be("Licitacao");
    }

    [Fact] // I-11 + Cenario 12: prazo exclui o dia inicial e inclui o final (art. 66, 30 dias).
    public void Invariante_11_prazo_exclui_dia_inicial_inclui_final()
    {
        var processo = NovoProcesso();

        processo.Prazo.Inicio.Should().Be(Hoje);
        processo.Prazo.Fim.Should().Be(Hoje.AddDays(Processo.PrazoDecisaoPadraoDias));
    }

    [Fact] // I-4: tramitar exige processo em andamento; sobre Sobrestado falha (B-4).
    public void Invariante_4_tramitar_exige_em_andamento()
    {
        var processo = NovoProcesso();
        processo.Sobrestar("Aguardando parecer");

        var acao = () => processo.Tramitar(Guid.NewGuid(), null, Hoje);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-4: tramitar sem setor de destino e rejeitado.
    public void Invariante_4_tramitar_sem_setor_destino_e_rejeitado()
    {
        var processo = NovoProcesso();

        var acao = () => processo.Tramitar(Guid.Empty, null, Hoje);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-6 + B-4: processo sobrestado nao tramita ate reativacao.
    public void Invariante_6_sobrestado_nao_tramita()
    {
        var processo = NovoProcesso();
        processo.Sobrestar("Diligencia");

        ((Action)(() => processo.Tramitar(Guid.NewGuid(), null, Hoje)))
            .Should().Throw<InvalidOperationException>();
        processo.Situacao.Should().Be(SituacaoProcesso.Sobrestado);
    }

    [Fact] // I-7 + B-5: arquivar processo ja arquivado falha.
    public void Invariante_7_arquivar_processo_ja_arquivado_falha()
    {
        var processo = NovoProcesso();
        processo.Arquivar("Concluido", Hoje);

        var acao = () => processo.Arquivar("De novo", Hoje);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8 + Cenario 5: estado terminal (Arquivado) nao admite tramitar/despachar/sobrestar.
    public void Invariante_8_arquivado_nao_admite_transicoes()
    {
        var processo = NovoProcesso();
        processo.Arquivar(null, Hoje);

        processo.Situacao.Should().Be(SituacaoProcesso.Arquivado);
        ((Action)(() => processo.Tramitar(Guid.NewGuid(), null, Hoje))).Should().Throw<InvalidOperationException>();
        ((Action)(() => processo.Despachar("Texto", Guid.NewGuid(), Hoje))).Should().Throw<InvalidOperationException>();
        ((Action)(() => processo.Sobrestar("Motivo"))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // B-6: sobrestar processo ja sobrestado falha (nao esta em andamento).
    public void Borda_6_sobrestar_processo_ja_sobrestado_falha()
    {
        var processo = NovoProcesso();
        processo.Sobrestar("Primeiro");

        var acao = () => processo.Sobrestar("Segundo");

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-5: sobrestar sem motivo e rejeitado.
    public void Invariante_5_sobrestar_sem_motivo_e_rejeitado()
    {
        var processo = NovoProcesso();

        var acao = () => processo.Sobrestar("   ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // B-7: reativar processo que nao esta sobrestado falha.
    public void Borda_7_reativar_processo_nao_sobrestado_falha()
    {
        var processo = NovoProcesso();

        var acao = () => processo.Reativar();

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // B-8: despachar processo arquivado falha (nao esta em andamento).
    public void Borda_8_despachar_processo_arquivado_falha()
    {
        var processo = NovoProcesso();
        processo.Arquivar(null, Hoje);

        var acao = () => processo.Despachar("Decisao", Guid.NewGuid(), Hoje);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-10 + B-13: despachos e movimentacoes sao append-only (nunca excluidos/editados).
    public void Invariante_10_despachos_e_movimentacoes_append_only()
    {
        var processo = NovoProcesso();
        processo.Despachar("Primeiro despacho", Guid.NewGuid(), Hoje);
        processo.Tramitar(Guid.NewGuid(), "Para o setor B", Hoje);
        processo.Despachar("Segundo despacho", Guid.NewGuid(), Hoje);

        processo.Despachos.Should().HaveCount(2);
        processo.Movimentacoes.Should().ContainSingle();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 4: Autuado --Tramitar--> EmTramitacao, registra Movimentacao e emite ProcessoTramitado.
    public void Transicao_tramitar_de_Autuado_para_EmTramitacao()
    {
        var processo = NovoProcesso();
        var setorDestino = Guid.NewGuid();

        processo.Tramitar(setorDestino, "Encaminhado", Hoje);

        processo.Situacao.Should().Be(SituacaoProcesso.EmTramitacao);
        processo.SetorAtualId.Should().Be(setorDestino);
        processo.Movimentacoes.Should().ContainSingle();
        processo.DomainEvents.OfType<ProcessoTramitado>().Should().ContainSingle()
            .Which.SetorDestinoId.Should().Be(setorDestino);
    }

    [Fact] // Despachar mantem a situacao (registro interno; sem evento proprio).
    public void Transicao_despachar_mantem_situacao()
    {
        var processo = NovoProcesso();
        processo.Tramitar(Guid.NewGuid(), null, Hoje);

        processo.Despachar("Defiro o pedido", Guid.NewGuid(), Hoje);

        processo.Situacao.Should().Be(SituacaoProcesso.EmTramitacao);
        processo.Despachos.Should().ContainSingle();
    }

    [Fact] // Cenario 6: Sobrestar e Reativar — Sobrestado --Reativar--> EmTramitacao.
    public void Transicao_sobrestar_e_reativar()
    {
        var processo = NovoProcesso();
        processo.Tramitar(Guid.NewGuid(), null, Hoje);

        processo.Sobrestar("Aguardando documento");
        processo.Situacao.Should().Be(SituacaoProcesso.Sobrestado);
        processo.DomainEvents.OfType<ProcessoSobrestado>().Should().ContainSingle();

        processo.Reativar();
        processo.Situacao.Should().Be(SituacaoProcesso.EmTramitacao);
        processo.DomainEvents.OfType<ProcessoTramitado>().Should().HaveCount(2);
    }

    [Fact] // Cenario 8: EmTramitacao --Arquivar--> Arquivado, emite ProcessoArquivado.
    public void Transicao_arquivar_de_EmTramitacao_para_Arquivado()
    {
        var processo = NovoProcesso();
        processo.Tramitar(Guid.NewGuid(), null, Hoje);

        processo.Arquivar("Decisao final", Hoje);

        processo.Situacao.Should().Be(SituacaoProcesso.Arquivado);
        processo.DomainEvents.OfType<ProcessoArquivado>().Should().ContainSingle();
    }

    [Fact] // Cenario 7: tentativa de tramitar processo sobrestado lanca InvalidOperationException.
    public void Cenario_7_tramitar_processo_sobrestado_lanca()
    {
        var processo = NovoProcesso();
        processo.Sobrestar("Suspenso");

        ((Action)(() => processo.Tramitar(Guid.NewGuid(), null, Hoje)))
            .Should().Throw<InvalidOperationException>();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 4 (persistencia): tramitacao persiste com auditoria e mantem o NUP.
    public async Task Cenario_4_fluxo_de_tramitacao_persiste_com_auditoria()
    {
        ProcessoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var processo = NovoProcesso();
            id = processo.Id;
            contexto.Processos.Add(processo);
            await contexto.SaveChangesAsync();

            processo.Tramitar(Guid.NewGuid(), "Encaminhado", Hoje);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var processo = await contexto.Processos
                .Include(p => p.Movimentacoes)
                .SingleAsync(p => p.Id == id);

            processo.Situacao.Should().Be(SituacaoProcesso.EmTramitacao);
            processo.TenantId.Should().Be(TenantA);
            processo.Nup.Valor.Should().Be("NUP-2026-0001");
            processo.Movimentacoes.Should().ContainSingle();

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes do processo");
        }
    }

    [Fact] // I-1 + Cenario 3 + B-3: indice unico (TenantId, Nup) impede NUP duplicado no mesmo tenant.
    public async Task Cenario_3_nup_duplicado_no_mesmo_tenant_e_rejeitado()
    {
        await using var contexto = CriarContexto(TenantA);
        contexto.Processos.Add(NovoProcesso(nup: "NUP-DUP"));
        await contexto.SaveChangesAsync();

        contexto.Processos.Add(NovoProcesso(nup: "NUP-DUP"));

        var acao = async () => await contexto.SaveChangesAsync();
        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // Cenario 11: consulta por setor e tenant-scoped (Global Query Filter).
    public async Task Cenario_11_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Processos.Add(NovoProcesso(nup: "NUP-A"));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Processos.ToListAsync()).Should().BeEmpty();
        }
    }
}
