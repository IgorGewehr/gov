using FluentAssertions;
using Tensorroot.Gov.Integracoes.Cnab;
using Xunit;

namespace Tensorroot.Gov.Integracoes.Tests;

/// <summary>Prova do gerador CNAB 240 (FEBRABAN): estrutura e largura fixa de 240 posições.</summary>
public sealed class Cnab240Tests
{
    [Fact]
    public void GerarRemessaPagamento_produz_arquivo_240_com_estrutura_febraban()
    {
        var empresa = new EmpresaCnab("11.222.333/0001-81", "Prefeitura de Exemplo", 1, "1234", "56789", "0");
        var pagamentos = new[]
        {
            new PagamentoCnab("Fornecedor A LTDA", "11.222.333/0001-81", 1, "1234", "111", 1500.50m, new DateOnly(2026, 6, 25)),
            new PagamentoCnab("Servidor B", "529.982.247-25", 104, "0001", "222", 3200.00m, new DateOnly(2026, 6, 25)),
        };

        var conteudo = new Cnab240Gerador().GerarRemessaPagamento(
            empresa, pagamentos, numeroRemessa: 1, geracao: new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc));

        var linhas = conteudo.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        linhas.Should().HaveCount(6); // header arquivo + header lote + 2 detalhes + trailer lote + trailer arquivo
        linhas.Should().OnlyContain(linha => linha.Length == 240, "todo registro CNAB 240 tem 240 posições");
        linhas[0][7].Should().Be('0', "tipo de registro do header de arquivo");
        linhas[1][7].Should().Be('1', "header de lote");
        linhas[2][7].Should().Be('3', "registro de detalhe");
        linhas[2][13].Should().Be('A', "segmento A");
        linhas[4][7].Should().Be('5', "trailer de lote");
        linhas[5][7].Should().Be('9', "trailer de arquivo");
        linhas[0][..3].Should().Be("001", "código do banco nas posições 1-3");
    }
}
