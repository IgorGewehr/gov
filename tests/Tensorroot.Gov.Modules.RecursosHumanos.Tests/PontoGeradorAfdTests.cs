using System.Text;
using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura da geracao POSICIONAL do AFD/AEJ (<see cref="GeradorAfd"/>/<see cref="GeradorAej"/>):
/// estrutura cabecalho/registros/trailer, ordenacao por NSR, codificacao ISO-8859-1, terminador CR+LF
/// e CRC-16. As POSICOES exatas seguem // TODO(validar-oficial) — os testes fixam o CONCEITO atual.
/// </summary>
public sealed class PontoGeradorAfdTests
{
    private static readonly Cnpj CnpjEnte = Cnpj.Create("11222333000181");
    private static readonly Cpf CpfA = Cpf.Create("39053344705");
    private static readonly Cpf CpfB = Cpf.Create("11144477735");

    private static CabecalhoAfd Cabecalho()
        => new(CnpjEnte, "MUNICIPIO DE TESTE", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30),
            new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero));

    [Fact] // Estrutura: 1 cabecalho + N marcacoes + 1 trailer; todas as linhas terminam em CR+LF.
    public void Afd_tem_cabecalho_marcacoes_e_trailer_com_crlf()
    {
        var linhas = new[]
        {
            new LinhaMarcacaoAfd(Nsr.De(1), CpfA, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), TipoRep.RepP),
            new LinhaMarcacaoAfd(Nsr.De(2), CpfA, new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), TipoRep.RepP),
        };

        var bytes = GeradorAfd.Gerar(Cabecalho(), linhas);
        var texto = Encoding.Latin1.GetString(bytes);
        var linhasTexto = texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        linhasTexto.Should().HaveCount(4); // cabecalho + 2 marcacoes + trailer
        linhasTexto[0].Should().StartWith("1"); // tipo cabecalho
        linhasTexto[1].Should().StartWith("7"); // tipo marcacao REP-P (3 = REP-C/A; 7 = REP-P)
        linhasTexto[^1].Should().StartWith("9"); // tipo trailer
        texto.Should().EndWith("\r\n");
    }

    [Fact] // As marcacoes saem ORDENADAS por NSR mesmo recebidas fora de ordem.
    public void Afd_ordena_marcacoes_por_nsr()
    {
        var linhas = new[]
        {
            new LinhaMarcacaoAfd(Nsr.De(3), CpfA, new DateTimeOffset(2026, 6, 1, 13, 0, 0, TimeSpan.Zero), TipoRep.RepP),
            new LinhaMarcacaoAfd(Nsr.De(1), CpfB, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), TipoRep.RepP),
            new LinhaMarcacaoAfd(Nsr.De(2), CpfA, new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), TipoRep.RepP),
        };

        var texto = Encoding.Latin1.GetString(GeradorAfd.Gerar(Cabecalho(), linhas));
        var marcacoes = texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.StartsWith('7')) // marcacao de REP-P = tipo 7 (3 = REP-C/A).
            .ToList();

        // NSR ocupa as posicoes 2..10 (largura 9) logo apos o tipo (1 char).
        marcacoes[0].Substring(1, 9).Should().Be("000000001");
        marcacoes[1].Substring(1, 9).Should().Be("000000002");
        marcacoes[2].Substring(1, 9).Should().Be("000000003");
    }

    [Fact] // O trailer carrega a quantidade de marcacoes e o CRC-16 (4 hex).
    public void Afd_trailer_totaliza_quantidade_e_carrega_crc()
    {
        var linhas = new[]
        {
            new LinhaMarcacaoAfd(Nsr.De(1), CpfA, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), TipoRep.RepP),
        };

        var texto = Encoding.Latin1.GetString(GeradorAfd.Gerar(Cabecalho(), linhas));
        var trailer = texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Single(l => l.StartsWith('9'));

        trailer.Substring(1, 9).Should().Be("000000001"); // 1 marcacao
        trailer[^4..].Should().MatchRegex("^[0-9A-F]{4}$"); // CRC hex
    }

    [Fact] // O AFD e codificavel em ISO-8859-1 (texto ASCII estendido — Portaria 671).
    public void Afd_e_codificado_em_iso_8859_1()
    {
        var linhas = new[]
        {
            new LinhaMarcacaoAfd(Nsr.De(1), CpfA, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), TipoRep.RepP),
        };
        var cabecalho = Cabecalho() with { RazaoSocial = "PREFEITURA SAO JOAO" };

        var acao = () => GeradorAfd.Gerar(cabecalho, linhas);

        acao.Should().NotThrow();
    }

    [Fact] // CRC-16/CCITT-FALSE: valor conhecido de "123456789" = 0x29B1.
    public void Crc16_ccitt_false_confere_vetor_conhecido()
    {
        var dados = Encoding.ASCII.GetBytes("123456789");

        var crc = Crc16Ccitt.Calcular(dados);

        crc.Should().Be(0x29B1);
    }

    [Fact] // AEJ: cabecalho + linhas diarias tratadas + trailer.
    public void Aej_tem_cabecalho_linhas_e_trailer()
    {
        var cabecalho = new CabecalhoAej(CnpjEnte, "MUNICIPIO DE TESTE",
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero));
        var linhas = new[]
        {
            new LinhaAej(CpfA, new DateOnly(2026, 6, 1), 480, 540, 60, 0),
            new LinhaAej(CpfA, new DateOnly(2026, 6, 2), 480, 450, 0, 30),
        };

        var texto = Encoding.Latin1.GetString(GeradorAej.Gerar(cabecalho, linhas));
        var linhasTexto = texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        linhasTexto.Should().HaveCount(4); // cabecalho + 2 dias + trailer
        linhasTexto[0].Should().StartWith("1");
        linhasTexto[^1].Should().StartWith("9");
    }
}
