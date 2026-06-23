using FluentAssertions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Testes-chave do dominio e-SIC (Onda 2 Transparencia, design §1.7; LAI Lei 12.527/2011): prazo calculado
/// (20 dias uteis), prorrogacao unica e so antes do vencimento, resposta apos prazo marca atraso,
/// indeferimento fundamentado, e mascaramento LGPD de CPF na superficie publica.
/// </summary>
public sealed class EsicDominioTests
{
    /// <summary>Calendario de teste deterministico: pula fins de semana (sem feriados).</summary>
    private sealed class CalendarioSemFeriados : ICalendarioDiasUteis
    {
        public DateOnly SomarDiasUteis(DateOnly inicio, int diasUteis)
        {
            var data = inicio;
            var restantes = diasUteis;
            while (restantes > 0)
            {
                data = data.AddDays(1);
                if (data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                {
                    restantes--;
                }
            }

            return data;
        }
    }

    private static PedidoInformacaoSic AbrirPedido(DateOnly abertura)
        => PedidoInformacaoSic.Abrir(
            Guid.NewGuid(),
            ProtocoloSic.Gerar(abertura.Year, 1),
            Solicitante.Criar("Maria Cidada", documento: "12345678909", contato: "maria@exemplo.org"),
            "Quero a relacao de despesas com diarias de 2026.",
            FormaResposta.Email,
            abertura,
            new CalendarioSemFeriados());

    [Fact]
    public void Abrir_calcula_prazo_de_20_dias_uteis_nunca_digitado()
    {
        // Segunda 2026-06-01 + 20 dias uteis = 2026-06-29 (4 semanas).
        var pedido = AbrirPedido(new DateOnly(2026, 6, 1));

        pedido.Situacao.Should().Be(SituacaoPedidoSic.Aberto);
        pedido.PrazoResposta.Should().Be(new DateOnly(2026, 6, 29));
        pedido.Protocolo.Valor.Should().Be("2026/000001");
    }

    [Fact]
    public void Prorrogacao_e_unica_e_so_antes_do_vencimento()
    {
        var pedido = AbrirPedido(new DateOnly(2026, 6, 1));

        // Antes do vencimento: ok, +10 dias uteis.
        pedido.Prorrogar("Volume elevado de documentos.", new DateOnly(2026, 6, 10), new CalendarioSemFeriados());
        pedido.ProrrogadoAte.Should().Be(new DateOnly(2026, 7, 13));

        // Segunda prorrogacao: proibida (unica — LAI art. 11 §2o).
        var segunda = () => pedido.Prorrogar("de novo", new DateOnly(2026, 6, 11), new CalendarioSemFeriados());
        segunda.Should().Throw<InvalidOperationException>().WithMessage("*ja foi concedida*");
    }

    [Fact]
    public void Prorrogar_apos_o_vencimento_e_rejeitado()
    {
        var pedido = AbrirPedido(new DateOnly(2026, 6, 1));

        var acao = () => pedido.Prorrogar("tarde demais", new DateOnly(2026, 6, 30), new CalendarioSemFeriados());

        acao.Should().Throw<InvalidOperationException>().WithMessage("*antes do vencimento*");
    }

    [Fact]
    public void Responder_apos_o_prazo_marca_em_atraso_mas_nao_bloqueia()
    {
        var pedido = AbrirPedido(new DateOnly(2026, 6, 1)); // prazo 2026-06-29
        pedido.IniciarAtendimento();

        // Resposta apos o prazo: aceita (nao bloqueia), mas o pedido fica Respondido.
        pedido.Responder(RespostaSic.Criar("Segue a relacao solicitada.", new DateOnly(2026, 7, 5)));

        pedido.Situacao.Should().Be(SituacaoPedidoSic.Respondido);
        (pedido.Resposta!.Data > pedido.PrazoVigente).Should().BeTrue();
    }

    [Fact]
    public void Indeferir_exige_fundamento_legal()
    {
        var pedido = AbrirPedido(new DateOnly(2026, 6, 1));

        var semFundamento = () => pedido.Indeferir("  ", new DateOnly(2026, 6, 5));
        semFundamento.Should().Throw<ArgumentException>();

        pedido.Indeferir("Informacao sigilosa — LAI art. 23 VIII.", new DateOnly(2026, 6, 5));
        pedido.Situacao.Should().Be(SituacaoPedidoSic.Indeferido);
        pedido.FundamentoIndeferimento.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Mascaramento_lgpd_oculta_cpf_e_preserva_cnpj_publico()
    {
        // CPF (11 digitos) sempre mascarado na superficie publica.
        Mascaramento.MascararDocumento("12345678909").Should().Be("***.456.789-**");

        // CNPJ (14 digitos) e dado publico de pessoa juridica — formatado, nao mascarado.
        Mascaramento.MascararDocumento("11222333000181").Should().Be("11.222.333/0001-81");

        // Vazio vira marcador opaco.
        Mascaramento.MascararDocumento(null).Should().Be(Mascaramento.SemDocumento);
    }
}
