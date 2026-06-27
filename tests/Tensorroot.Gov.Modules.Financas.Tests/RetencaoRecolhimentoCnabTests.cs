using FluentAssertions;
using Tensorroot.Gov.Modules.Financas.Domain.Cnab;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Recolhimentos;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Invariantes das consignações (retenções na fonte), do recolhimento extra-orçamentário e da remessa
/// bancária CNAB240. Base legal: IN RFB 1.234/2012 (IRRF de serviços, dispensa por DARF mínimo — art. 3º,
/// §6º); Lei 4.320/1964 (ingressos/dispêndios extra-orçamentários); layout FEBRABAN CNAB240.
/// </summary>
public sealed class RetencaoRecolhimentoCnabTests
{
    private static TabelaIrrfServicos TabelaVigente(decimal valorMinimo = 10m) =>
        TabelaIrrfServicos.Criar(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            vigenciaFim: null,
            valorMinimoRetencao: valorMinimo,
            faixas: new[]
            {
                // Serviços profissionais — 1,5% (IN RFB 1.234/2012, Anexo I).
                FaixaIrrfServicos.De("SERV_PROF", "Servicos profissionais", 0.015m, "1708"),
            });

    [Fact]
    public void Irrf_servicos_retem_aliquota_sobre_a_base()
    {
        var tabela = TabelaVigente();

        var apuracao = tabela.Apurar("SERV_PROF", ValorMonetario.De(10_000m));

        apuracao.ValorRetido.Valor.Should().Be(150m, "1,5% de 10.000 = 150,00");
        apuracao.DispensadoPorMinimo.Should().BeFalse();
        apuracao.CodigoReceitaDarf.Should().Be("1708");
    }

    [Fact]
    public void Irrf_abaixo_do_minimo_dispensa_retencao()
    {
        // 1,5% de 100,00 = 1,50 < mínimo de R$ 10,00 (IN 1.234/2012, art. 3º, §6º) ⇒ retém zero.
        var tabela = TabelaVigente(valorMinimo: 10m);

        var apuracao = tabela.Apurar("SERV_PROF", ValorMonetario.De(100m));

        apuracao.ValorRetido.Valor.Should().Be(0m);
        apuracao.DispensadoPorMinimo.Should().BeTrue();
    }

    [Fact]
    public void Guia_reune_retencoes_recolhe_e_emite_evento_extra_orcamentario()
    {
        var tenantId = Guid.NewGuid();
        var guia = GuiaRecolhimento.Emitir(
            tenantId,
            NaturezaRetencao.IrrfPessoaJuridica,
            codigoReceita: "1708",
            favorecidoDocumento: "00000000000191",
            dataVencimento: new DateOnly(2026, 7, 20),
            competencia: new DateOnly(2026, 6, 1));

        guia.AdicionarItem(new ItemGuiaRecolhimento(LiquidacaoId.New(), RetencaoId.New(), 150m));
        guia.AdicionarItem(new ItemGuiaRecolhimento(LiquidacaoId.New(), RetencaoId.New(), 90m));

        guia.ValorTotal.Valor.Should().Be(240m, "o total da guia e a soma dos itens (I-4)");

        guia.Recolher(new DateOnly(2026, 7, 18));

        guia.Situacao.Should().Be(SituacaoGuiaRecolhimento.Recolhida);
        guia.DataRecolhimento.Should().Be(new DateOnly(2026, 7, 18));
        guia.DomainEvents.OfType<RecolhimentoEfetuado>().Should().ContainSingle()
            .Which.Valor.Should().Be(240m);
    }

    [Fact]
    public void Guia_vazia_nao_pode_ser_recolhida()
    {
        var guia = GuiaRecolhimento.Emitir(
            Guid.NewGuid(),
            NaturezaRetencao.IssRetido,
            codigoReceita: null,
            favorecidoDocumento: null,
            dataVencimento: new DateOnly(2026, 7, 20),
            competencia: new DateOnly(2026, 6, 1));

        var recolher = () => guia.Recolher(new DateOnly(2026, 7, 18));

        recolher.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Remessa_cnab240_gera_registros_de_240_posicoes()
    {
        var remessa = new RemessaCnab240(
            new PagadorCnab("001", TipoInscricaoCnab.Cnpj, "00000000000191", "1234567", "1234", "5", "98765", "4", "3", "MUNICIPIO DE TESTE"),
            FormaLancamentoCnab.CreditoContaCorrente,
            new DateOnly(2026, 7, 18),
            new TimeOnly(10, 30),
            SequencialArquivo: 1,
            Favorecidos: new[]
            {
                new FavorecidoCnab("237", "4321", "1", "112233", "0", "FORNECEDOR LTDA", TipoInscricaoCnab.Cnpj, "11222333000181", "OP-1", new DateOnly(2026, 7, 18), 1234.56m),
            });

        var conteudo = Cnab240Writer.Gerar(remessa);
        var linhas = conteudo.Split("\r\n");

        // header arquivo + header lote + (A,B) + trailer lote + trailer arquivo = 6 registros.
        linhas.Should().HaveCount(6);
        linhas.Should().OnlyContain(l => l.Length == 240, "cada registro CNAB240 tem exatamente 240 posicoes (I-1)");
    }

    [Fact]
    public void Remessa_cnab240_sem_favorecidos_falha()
    {
        var remessa = new RemessaCnab240(
            new PagadorCnab("001", TipoInscricaoCnab.Cnpj, "00000000000191", "1234567", "1234", "5", "98765", "4", "3", "MUNICIPIO DE TESTE"),
            FormaLancamentoCnab.CreditoContaCorrente,
            new DateOnly(2026, 7, 18),
            new TimeOnly(10, 30),
            SequencialArquivo: 1,
            Favorecidos: Array.Empty<FavorecidoCnab>());

        var gerar = () => Cnab240Writer.Gerar(remessa);

        gerar.Should().Throw<RemessaCnabInvalidaException>();
    }
}
