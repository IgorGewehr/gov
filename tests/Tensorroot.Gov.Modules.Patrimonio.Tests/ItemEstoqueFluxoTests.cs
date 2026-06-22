using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Cobertura de integração do agregado <see cref="ItemEstoque"/>: invariantes de almoxarifado,
/// cada transição da máquina de estados e os cenários BDD de ItemEstoque.rules.md,
/// sobre SQLite em memória com auditoria, Outbox e isolamento por tenant.
/// </summary>
public sealed class ItemEstoqueFluxoTests : PatrimonioTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    private static ItemEstoque NovoItem(
        MetodoCusteio metodo = MetodoCusteio.Peps,
        decimal pontoPedido = 5m,
        string codigo = "ALM-001")
        => ItemEstoque.Cadastrar(
            TenantA,
            codigo,
            "Resma de papel A4",
            "cx",
            metodo,
            PontoPedido.De(pontoPedido),
            CurvaABC.B);

    // ---------- Invariantes ----------

    [Fact] // I-10 + Cenário 7: cadastro nasce Ativo, com saldo zero.
    public void Invariante_10_cadastro_nasce_ativo_com_saldo_zero()
    {
        var item = NovoItem();

        item.Situacao.Should().Be(SituacaoItemEstoque.Ativo);
        item.Saldo.Quantidade.Should().Be(0m);
        item.Movimentavel.Should().BeTrue();
    }

    [Fact] // I-5 + Cenário 7: entrada cria lote, incrementa saldo (PEPS).
    public void Invariante_5_entrada_cria_lote_e_incrementa_saldo()
    {
        var item = NovoItem();

        item.RegistrarEntrada(10m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        item.Saldo.Quantidade.Should().Be(10m);
        item.Lotes.Should().ContainSingle();
        item.Movimentos.Should().ContainSingle()
            .Which.Tipo.Should().Be(TipoMovimento.Entrada);
    }

    [Fact] // I-5 (médio): entrada recalcula o custo médio ponderado.
    public void Invariante_5_entrada_recalcula_custo_medio()
    {
        var item = NovoItem(MetodoCusteio.Medio);

        item.RegistrarEntrada(10m, ValorMonetario.De(2m), Hoje, null, "NF-1");
        item.RegistrarEntrada(10m, ValorMonetario.De(4m), Hoje, null, "NF-2");

        // (10*2 + 10*4) / 20 = 3.
        item.CustoMedio.Valor.Should().Be(3m);
    }

    [Fact] // I-4 + Cenário 5: a entrada NÃO reconhece despesa (sem movimento de saída).
    public void Invariante_4_entrada_nao_reconhece_despesa()
    {
        var item = NovoItem();

        item.RegistrarEntrada(10m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        item.Movimentos.Should().OnlyContain(m => m.Tipo == TipoMovimento.Entrada);
    }

    [Fact] // I-1 + Cenário 2: saída além do saldo é rejeitada e o saldo permanece.
    public void Invariante_1_saida_alem_do_saldo_e_rejeitada()
    {
        var item = NovoItem();
        item.RegistrarEntrada(10m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        var acao = () => item.AtenderRequisicao(RequisicaoId.New(), Guid.NewGuid(), 15m, Hoje);

        acao.Should().Throw<InvalidOperationException>();
        item.Saldo.Quantidade.Should().Be(10m);
    }

    [Fact] // I-1 + B-1: saída exatamente igual ao saldo é permitida (resulta em zero).
    public void Invariante_1_saida_igual_ao_saldo_zera()
    {
        var item = NovoItem(pontoPedido: 0m);
        item.RegistrarEntrada(10m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        item.AtenderRequisicao(RequisicaoId.New(), Guid.NewGuid(), 10m, Hoje);

        item.Saldo.Quantidade.Should().Be(0m);
    }

    [Fact] // I-3/I-8 + Cenário 3: PEPS consome lotes na ordem de entrada.
    public void Invariante_8_peps_consome_lotes_mais_antigos_primeiro()
    {
        var item = NovoItem(MetodoCusteio.Peps);
        item.RegistrarEntrada(10m, ValorMonetario.De(2m), new DateOnly(2026, 1, 1), null, "NF-A");
        item.RegistrarEntrada(10m, ValorMonetario.De(3m), new DateOnly(2026, 2, 1), null, "NF-B");

        var custo = item.AtenderRequisicao(RequisicaoId.New(), Guid.NewGuid(), 15m, Hoje);

        // 10*2 + 5*3 = 35.
        custo.Valor.Should().Be(35m);
        item.Saldo.Quantidade.Should().Be(5m);
    }

    [Fact] // I-3 + Cenário 4: custeio médio valora a saída pelo CustoMedio.
    public void Invariante_3_custeio_medio_valora_saida()
    {
        var item = NovoItem(MetodoCusteio.Medio);
        item.RegistrarEntrada(10m, ValorMonetario.De(2.5m), Hoje, null, "NF-1");

        var custo = item.AtenderRequisicao(RequisicaoId.New(), Guid.NewGuid(), 4m, Hoje);

        // 4 * 2.50 = 10.00.
        custo.Valor.Should().Be(10m);
    }

    [Fact] // I-7 + Cenário 8: atendimento gera saída, reduz saldo e emite RequisicaoAtendida.
    public void Invariante_7_atendimento_emite_requisicao_atendida()
    {
        var item = NovoItem(pontoPedido: 2m);
        item.RegistrarEntrada(20m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        item.AtenderRequisicao(RequisicaoId.New(), Guid.NewGuid(), 5m, Hoje);

        item.Saldo.Quantidade.Should().Be(15m);
        item.DomainEvents.OfType<RequisicaoAtendida>().Should().ContainSingle();
        // saldo (15) ainda acima do ponto de pedido (2): NÃO emite PontoPedidoAtingido.
        item.DomainEvents.OfType<PontoPedidoAtingido>().Should().BeEmpty();
    }

    [Fact] // I-6 + Cenário 1 + B-2: saída que atinge o ponto de pedido emite PontoPedidoAtingido.
    public void Invariante_6_ponto_de_pedido_atingido_emite_evento()
    {
        var item = NovoItem(pontoPedido: 5m);
        item.RegistrarEntrada(10m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        item.AtenderRequisicao(RequisicaoId.New(), Guid.NewGuid(), 6m, Hoje);

        // saldo resultante 4 <= ponto 5.
        item.Saldo.Quantidade.Should().Be(4m);
        item.DomainEvents.OfType<PontoPedidoAtingido>().Should().ContainSingle();
    }

    [Fact] // I-2 + Cenário 6: VRL inferior ao custo reduz a mensuração ao VRL.
    public void Invariante_2_menor_entre_custo_e_vrl()
    {
        var item = NovoItem(MetodoCusteio.Medio);
        item.RegistrarEntrada(10m, ValorMonetario.De(5m), Hoje, null, "NF-1");

        item.AjustarValorRealizavelLiquido(ValorMonetario.De(3m), Hoje);

        item.CustoMedio.Valor.Should().Be(3m);
        item.ValorRealizavelLiquido!.Valor.Should().Be(3m);
    }

    [Fact] // I-2 + B-5: VRL >= custo mantém a mensuração pelo custo.
    public void Invariante_2_vrl_acima_do_custo_mantem_custo()
    {
        var item = NovoItem(MetodoCusteio.Medio);
        item.RegistrarEntrada(10m, ValorMonetario.De(5m), Hoje, null, "NF-1");

        item.AjustarValorRealizavelLiquido(ValorMonetario.De(8m), Hoje);

        item.CustoMedio.Valor.Should().Be(5m);
    }

    [Fact] // I-9 + Cenário 9 + B-6: item inativo não admite saída.
    public void Invariante_9_item_inativo_nao_admite_saida()
    {
        var item = NovoItem();
        item.Inativar();

        var acao = () => item.AtenderRequisicao(RequisicaoId.New(), Guid.NewGuid(), 1m, Hoje);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-9: item inativo não admite entrada.
    public void Invariante_9_item_inativo_nao_admite_entrada()
    {
        var item = NovoItem();
        item.Inativar();

        var acao = () => item.RegistrarEntrada(1m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-11 + B-8: quantidade não positiva em entrada é rejeitada.
    public void Invariante_11_quantidade_nao_positiva_em_entrada_e_rejeitada()
    {
        var item = NovoItem();

        var acao = () => item.RegistrarEntrada(0m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-11 + B-8: quantidade não positiva em requisição é rejeitada.
    public void Invariante_11_quantidade_nao_positiva_em_requisicao_e_rejeitada()
    {
        var item = NovoItem();
        item.RegistrarEntrada(10m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        var acao = () => item.AtenderRequisicao(RequisicaoId.New(), Guid.NewGuid(), -1m, Hoje);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // Cenário 10 + B-7: inativação exige saldo zero.
    public void Inativacao_com_saldo_remanescente_e_rejeitada()
    {
        var item = NovoItem();
        item.RegistrarEntrada(10m, ValorMonetario.De(2m), Hoje, null, "NF-1");

        var acao = item.Inativar;

        acao.Should().Throw<InvalidOperationException>();
        item.Situacao.Should().Be(SituacaoItemEstoque.Ativo);
    }

    // ---------- Máquina de estados (transições) ----------

    [Fact] // Ativo --ReclassificarAbc--> Ativo.
    public void Transicao_reclassificar_abc_mantem_ativo()
    {
        var item = NovoItem();

        item.ReclassificarAbc(CurvaABC.A);

        item.ClassificacaoAbc.Should().Be(CurvaABC.A);
        item.Situacao.Should().Be(SituacaoItemEstoque.Ativo);
    }

    [Fact] // Ativo --InativarItem--> Inativo (saldo zero).
    public void Transicao_inativar_com_saldo_zero_para_inativo()
    {
        var item = NovoItem();

        item.Inativar();

        item.Situacao.Should().Be(SituacaoItemEstoque.Inativo);
        item.Movimentavel.Should().BeFalse();
    }

    [Fact] // Inativar duas vezes falha.
    public void Transicao_inativar_item_ja_inativo_falha()
    {
        var item = NovoItem();
        item.Inativar();

        var acao = item.Inativar;

        acao.Should().Throw<InvalidOperationException>();
    }

    // ---------- Persistência, auditoria e isolamento ----------

    [Fact] // B-9: código duplicado por tenant viola o índice único (TenantId, Codigo).
    public async Task Codigo_duplicado_no_tenant_viola_indice_unico()
    {
        await using var contexto = CriarContexto(TenantA);
        contexto.ItensEstoque.Add(NovoItem(codigo: "ALM-DUP"));
        await contexto.SaveChangesAsync();

        contexto.ItensEstoque.Add(NovoItem(codigo: "ALM-DUP"));
        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // Fluxo completo (entrada + saída) persiste lotes/movimentos e gera auditoria.
    public async Task Fluxo_de_almoxarifado_persiste_com_auditoria()
    {
        ItemEstoqueId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var item = NovoItem(pontoPedido: 5m);
            id = item.Id;
            item.RegistrarEntrada(10m, ValorMonetario.De(2m), Hoje, null, "NF-1");
            contexto.ItensEstoque.Add(item);
            await contexto.SaveChangesAsync();

            item.AtenderRequisicao(RequisicaoId.New(), Guid.NewGuid(), 6m, Hoje);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var item = await contexto.ItensEstoque.SingleAsync(i => i.Id == id);
            item.Saldo.Quantidade.Should().Be(4m);
            item.Movimentos.Should().HaveCount(2);
            item.Requisicoes.Should().ContainSingle()
                .Which.Situacao.Should().Be(SituacaoRequisicao.Atendida);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutações do item");
        }
    }

    [Fact] // Cenário 12: consulta é tenant-scoped (Global Query Filter).
    public async Task Cenario_12_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.ItensEstoque.Add(NovoItem());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.ItensEstoque.ToListAsync()).Should().BeEmpty();
        }
    }
}
