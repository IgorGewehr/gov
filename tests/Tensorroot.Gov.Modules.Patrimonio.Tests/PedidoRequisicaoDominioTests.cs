using FluentAssertions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Patrimonio.Tests;

/// <summary>
/// Testes-chave de dominio do agregado <see cref="PedidoRequisicao"/> (Onda 3b — almoxarifado
/// self-service): fluxo Solicitado -> Aprovado -> Atendido, consolidacao de linhas (R-1), guardas de
/// transicao (R-2/R-3/R-4) e cancelamento (R-5). Dominio puro — sem persistencia.
/// </summary>
public sealed class PedidoRequisicaoDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Unidade = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Solicitante = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Aprovador = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly DateOnly Hoje = new(2026, 6, 23);

    private static PedidoRequisicao AbrirPedido(params (ItemEstoqueId Item, decimal Qtd)[] linhas)
        => PedidoRequisicao.Abrir(Tenant, Unidade, "Almoxarifado Central", Solicitante, Hoje, "reposicao", linhas);

    [Fact]
    public void Abrir_pedido_valido_deve_nascer_solicitado_e_emitir_evento()
    {
        var item = ItemEstoqueId.New();

        var pedido = AbrirPedido((item, 10m));

        pedido.Situacao.Should().Be(SituacaoPedido.Solicitado);
        pedido.TenantId.Should().Be(Tenant);
        pedido.UnidadeId.Should().Be(Unidade);
        pedido.Itens.Should().ContainSingle();
        pedido.DomainEvents.Should().ContainSingle(e => e is PedidoRequisicaoAberto);
    }

    [Fact]
    public void Abrir_pedido_consolida_linhas_repetidas_do_mesmo_item()
    {
        var item = ItemEstoqueId.New();

        var pedido = AbrirPedido((item, 4m), (item, 6m));

        pedido.Itens.Should().ContainSingle();
        pedido.Itens.Single().QuantidadeSolicitada.Should().Be(10m);
    }

    [Fact]
    public void Abrir_pedido_sem_linhas_deve_falhar()
    {
        var act = () => AbrirPedido();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Aprovar_pedido_solicitado_deve_transitar_e_emitir_evento()
    {
        var pedido = AbrirPedido((ItemEstoqueId.New(), 10m));

        pedido.Aprovar(Aprovador, Hoje);

        pedido.Situacao.Should().Be(SituacaoPedido.Aprovado);
        pedido.AprovadorId.Should().Be(Aprovador);
        pedido.DomainEvents.Should().Contain(e => e is PedidoRequisicaoAprovado);
    }

    [Fact]
    public void Atender_pedido_nao_aprovado_deve_falhar()
    {
        var pedido = AbrirPedido((ItemEstoqueId.New(), 10m));
        var linhaId = pedido.Itens.Single().Id;

        var act = () => pedido.RegistrarAtendimentoDeLinha(linhaId, 5m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Atendimento_parcial_concluido_deve_fechar_em_atendido()
    {
        var pedido = AbrirPedido((ItemEstoqueId.New(), 10m));
        var linhaId = pedido.Itens.Single().Id;
        pedido.Aprovar(Aprovador, Hoje);

        pedido.RegistrarAtendimentoDeLinha(linhaId, 6m);
        pedido.ConcluirAtendimento(Hoje);

        pedido.Situacao.Should().Be(SituacaoPedido.Atendido);
        pedido.Itens.Single().QuantidadeAtendida.Should().Be(6m);
        pedido.Itens.Single().TotalmenteAtendido.Should().BeFalse();
        pedido.DomainEvents.Should().Contain(e => e is PedidoRequisicaoAtendido);
    }

    [Fact]
    public void Concluir_atendimento_sem_baixa_deve_falhar()
    {
        var pedido = AbrirPedido((ItemEstoqueId.New(), 10m));
        pedido.Aprovar(Aprovador, Hoje);

        var act = () => pedido.ConcluirAtendimento(Hoje);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancelar_pedido_atendido_deve_falhar()
    {
        var pedido = AbrirPedido((ItemEstoqueId.New(), 10m));
        var linhaId = pedido.Itens.Single().Id;
        pedido.Aprovar(Aprovador, Hoje);
        pedido.RegistrarAtendimentoDeLinha(linhaId, 10m);
        pedido.ConcluirAtendimento(Hoje);

        var act = () => pedido.Cancelar("desistencia");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancelar_pedido_solicitado_deve_transitar_para_cancelado()
    {
        var pedido = AbrirPedido((ItemEstoqueId.New(), 10m));

        pedido.Cancelar("setor extinto");

        pedido.Situacao.Should().Be(SituacaoPedido.Cancelado);
        pedido.MotivoCancelamento.Should().Be("setor extinto");
    }
}
