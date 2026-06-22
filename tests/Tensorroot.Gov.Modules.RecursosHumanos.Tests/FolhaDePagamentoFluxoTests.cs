using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="FolhaDePagamento"/>: invariantes, cada transicao
/// da maquina de estados (Aberta -> Calculada -> Fechada -> Paga), abate-teto, separacao RPPS/RGPS
/// e os cenarios BDD de FolhaDePagamento.rules.md, sobre SQLite em memoria com auditoria e
/// isolamento por tenant.
/// </summary>
public sealed class FolhaDePagamentoFluxoTests : RecursosHumanosTestBase
{
    private const decimal Teto = 30000m;
    private static readonly DateOnly Hoje = new(2026, 7, 10);

    private static FolhaDePagamento NovaFolha(int ano = 2026, int mes = 6)
        => FolhaDePagamento.Abrir(TenantA, Competencia.De(ano, mes));

    // ---------- Invariantes ----------

    [Fact] // I-14 + Cenario 1: abertura nasce Aberta e emite FolhaAberta.
    public void Invariante_14_abertura_nasce_aberta_e_emite_evento()
    {
        var folha = NovaFolha();

        folha.Situacao.Should().Be(SituacaoFolha.Aberta);
        folha.DomainEvents.OfType<FolhaAberta>().Should().ContainSingle();
    }

    [Fact] // I-2 + Cenario 4: lancamento bloqueado apos calculo.
    public void Invariante_2_lancamento_bloqueado_apos_calculo()
    {
        var folha = NovaFolha();
        folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);
        folha.Calcular(Teto, Hoje);

        var acao = () => folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("EXTRA"), TipoEvento.Provento, BaseCalculo.De(100m), 100m, RegimePrevidenciario.Rpps);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-5 + Cenario 5: calculo consolida totais e liquido.
    public void Invariante_5_calculo_consolida_totais()
    {
        var servidor = Guid.NewGuid();
        var folha = NovaFolha();
        folha.AdicionarEvento(servidor, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);
        folha.AdicionarEvento(servidor, Rubrica.De("INSS"), TipoEvento.Desconto, BaseCalculo.De(5000m), 800m, RegimePrevidenciario.Rpps);

        folha.Calcular(Teto, Hoje);

        folha.TotalProventos.Should().Be(5000m);
        folha.TotalDescontos.Should().Be(800m);
        folha.TotalLiquido.Valor.Should().Be(4200m);
        folha.DomainEvents.OfType<FolhaCalculada>().Should().ContainSingle();
    }

    [Fact] // I-6 + Cenario 6: abate-teto reduz o liquido ao limite constitucional.
    public void Invariante_6_abate_teto_reduz_liquido_ao_limite()
    {
        var servidor = Guid.NewGuid();
        var folha = NovaFolha();
        // Proventos 35000 > teto 30000 -> excesso 5000 lancado como desconto.
        folha.AdicionarEvento(servidor, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(35000m), 35000m, RegimePrevidenciario.Rpps);

        folha.Calcular(Teto, Hoje);

        folha.Eventos.Should().Contain(e => e.Tipo == TipoEvento.Desconto && e.Rubrica.Codigo == FolhaDePagamento.CodigoRubricaAbateTetoPadrao);
        folha.TotalDescontos.Should().Be(5000m);
        folha.TotalLiquido.Valor.Should().Be(30000m);
    }

    [Fact] // I-6 + B-5: proventos exatamente no teto nao geram abate-teto.
    public void Borda_5_proventos_no_teto_nao_geram_abate_teto()
    {
        var servidor = Guid.NewGuid();
        var folha = NovaFolha();
        folha.AdicionarEvento(servidor, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(Teto), Teto, RegimePrevidenciario.Rpps);

        folha.Calcular(Teto, Hoje);

        folha.Eventos.Should().NotContain(e => e.Rubrica.Codigo == FolhaDePagamento.CodigoRubricaAbateTetoPadrao);
        folha.TotalLiquido.Valor.Should().Be(Teto);
    }

    [Fact] // I-7 + Cenario 8: fechamento sem calculo e rejeitado.
    public void Invariante_7_fechamento_sem_calculo_e_rejeitado()
    {
        var folha = NovaFolha();

        var acao = () => folha.Fechar(Hoje);

        acao.Should().Throw<InvalidOperationException>();
        folha.Situacao.Should().Be(SituacaoFolha.Aberta);
    }

    [Fact] // I-9 + Cenario 11: pagamento sem fechamento e rejeitado.
    public void Invariante_9_pagamento_sem_fechamento_e_rejeitado()
    {
        var folha = NovaFolha();
        folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);
        folha.Calcular(Teto, Hoje);

        var acao = () => folha.EfetuarPagamento(new DateOnly(2026, 7, 5));

        acao.Should().Throw<InvalidOperationException>();
        folha.Situacao.Should().Be(SituacaoFolha.Calculada);
    }

    [Fact] // I-10 + Cenario / B-9: folha paga e terminal (nem recalcula, nem fecha, nem repaga).
    public void Invariante_10_folha_paga_e_terminal()
    {
        var folha = NovaFolha();
        folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);
        folha.Calcular(Teto, Hoje);
        folha.Fechar(Hoje);
        folha.EfetuarPagamento(new DateOnly(2026, 7, 20));

        folha.Situacao.Should().Be(SituacaoFolha.Paga);
        ((Action)(() => folha.Calcular(Teto, Hoje))).Should().Throw<InvalidOperationException>();
        ((Action)(() => folha.Fechar(Hoje))).Should().Throw<InvalidOperationException>();
        ((Action)(() => folha.EfetuarPagamento(new DateOnly(2026, 7, 21)))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-4 + Cenario 12: separacao RPPS/RGPS preservada nos eventos.
    public void Invariante_4_separacao_rpps_rgps()
    {
        var efetivo = Guid.NewGuid();
        var comissionado = Guid.NewGuid();
        var folha = NovaFolha();
        folha.AdicionarEvento(efetivo, Rubrica.De("VENC-RPPS"), TipoEvento.Provento, BaseCalculo.De(8000m), 8000m, RegimePrevidenciario.Rpps);
        folha.AdicionarEvento(comissionado, Rubrica.De("VENC-RGPS"), TipoEvento.Provento, BaseCalculo.De(6000m), 6000m, RegimePrevidenciario.Rgps);

        folha.Calcular(Teto, Hoje);

        folha.Eventos.Should().Contain(e => e.ServidorId == efetivo && e.RegimePrevidenciario == RegimePrevidenciario.Rpps);
        folha.Eventos.Should().Contain(e => e.ServidorId == comissionado && e.RegimePrevidenciario == RegimePrevidenciario.Rgps);
    }

    [Fact] // B-10: liquido nunca negativo — descontos acima dos proventos consolidam zero.
    public void Borda_10_liquido_nunca_negativo()
    {
        var servidor = Guid.NewGuid();
        var folha = NovaFolha();
        folha.AdicionarEvento(servidor, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(1000m), 1000m, RegimePrevidenciario.Rgps);
        folha.AdicionarEvento(servidor, Rubrica.De("PENSAO"), TipoEvento.Desconto, BaseCalculo.De(1000m), 1500m, RegimePrevidenciario.Rgps);

        folha.Calcular(Teto, Hoje);

        folha.TotalLiquido.Valor.Should().Be(0m);
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 3: AdicionarEvento em folha Aberta acrescenta o evento.
    public void Transicao_adicionar_evento_em_folha_aberta()
    {
        var folha = NovaFolha();

        folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);

        folha.Eventos.Should().ContainSingle();
        folha.Situacao.Should().Be(SituacaoFolha.Aberta);
    }

    [Fact] // B-6: recalcular folha Calculada e permitido (permanece Calculada, reaplica abate-teto).
    public void Transicao_recalcular_folha_calculada_e_permitido()
    {
        var servidor = Guid.NewGuid();
        var folha = NovaFolha();
        folha.AdicionarEvento(servidor, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(35000m), 35000m, RegimePrevidenciario.Rpps);
        folha.Calcular(Teto, Hoje);

        folha.Calcular(Teto, Hoje);

        folha.Situacao.Should().Be(SituacaoFolha.Calculada);
        // Apenas um abate-teto apos recalculo (idempotente).
        folha.Eventos.Count(e => e.Rubrica.Codigo == FolhaDePagamento.CodigoRubricaAbateTetoPadrao).Should().Be(1);
        folha.TotalLiquido.Valor.Should().Be(30000m);
    }

    [Fact] // Cenario 7: Calculada --Fechar--> Fechada, emite FolhaFechada.
    public void Transicao_fechar_de_Calculada_para_Fechada()
    {
        var folha = NovaFolha();
        folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);
        folha.Calcular(Teto, Hoje);

        folha.Fechar(Hoje);

        folha.Situacao.Should().Be(SituacaoFolha.Fechada);
        folha.DataFechamento.Should().Be(Hoje);
        folha.DomainEvents.OfType<FolhaFechada>().Should().ContainSingle();
    }

    [Fact] // B-8: fechar folha duas vezes — a segunda falha.
    public void Borda_8_fechar_folha_duas_vezes_falha()
    {
        var folha = NovaFolha();
        folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);
        folha.Calcular(Teto, Hoje);
        folha.Fechar(Hoje);

        var acao = () => folha.Fechar(Hoje);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // Cenario 10: Fechada --EfetuarPagamento--> Paga, emite PagamentoEfetuado.
    public void Transicao_pagar_de_Fechada_para_Paga()
    {
        var folha = NovaFolha();
        folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);
        folha.Calcular(Teto, Hoje);
        folha.Fechar(Hoje);

        folha.EfetuarPagamento(new DateOnly(2026, 7, 25));

        folha.Situacao.Should().Be(SituacaoFolha.Paga);
        folha.DataPagamento.Should().Be(new DateOnly(2026, 7, 25));
        folha.DomainEvents.OfType<PagamentoEfetuado>().Should().ContainSingle();
    }

    [Fact] // I-2: RemoverEvento em folha Aberta corrige lancamentos.
    public void Transicao_remover_evento_em_folha_aberta()
    {
        var folha = NovaFolha();
        var evento = folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);

        folha.RemoverEvento(evento.Id);

        folha.Eventos.Should().BeEmpty();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 7 (persistencia): fluxo Abrir->Calcular->Fechar persiste com auditoria e eventos-filhos.
    public async Task Fluxo_de_fechamento_persiste_com_auditoria()
    {
        FolhaDePagamentoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var folha = NovaFolha();
            folha.AdicionarEvento(Guid.NewGuid(), Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);
            id = folha.Id;
            contexto.FolhasDePagamento.Add(folha);
            await contexto.SaveChangesAsync();

            folha.Calcular(Teto, Hoje);
            folha.Fechar(Hoje);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var folha = await contexto.FolhasDePagamento
                .Include(f => f.Eventos)
                .SingleAsync(f => f.Id == id);
            folha.Situacao.Should().Be(SituacaoFolha.Fechada);
            folha.TenantId.Should().Be(TenantA);
            folha.Eventos.Should().NotBeEmpty();

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes da folha");
        }
    }

    [Fact] // I-1 + Cenario 2: competencia duplicada no tenant viola o indice unico.
    public async Task Cenario_2_competencia_duplicada_e_rejeitada()
    {
        await using var contexto = CriarContexto(TenantA);
        contexto.FolhasDePagamento.Add(NovaFolha(2026, 6));
        await contexto.SaveChangesAsync();

        contexto.FolhasDePagamento.Add(NovaFolha(2026, 6));

        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // Cenario 13: consulta de folha e isolada por tenant (Global Query Filter).
    public async Task Cenario_13_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.FolhasDePagamento.Add(NovaFolha(2026, 6));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.FolhasDePagamento.ToListAsync()).Should().BeEmpty();
        }
    }
}
