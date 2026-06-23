using System.Text;
using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura do <see cref="ParserAfd"/> (inverso do <see cref="GeradorAfd"/>): round-trip fiel
/// (gerar -&gt; parsear), validacao de CRC-16, continuidade de NSR (lacuna = suspeito), contador do
/// trailer e distincao tipo 3 (REP-C/A) vs 7 (REP-P). As POSICOES seguem // TODO(validar-oficial).
/// </summary>
public sealed class PontoParserAfdTests
{
    private static readonly Cnpj CnpjEnte = Cnpj.Create("11222333000181");
    private static readonly Cpf CpfA = Cpf.Create("39053344705");
    private static readonly Cpf CpfB = Cpf.Create("11144477735");
    private readonly ParserAfd _parser = new();

    private static CabecalhoAfd Cabecalho()
        => new(CnpjEnte, "MUNICIPIO DE TESTE", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30),
            new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero));

    [Fact] // Round-trip: o que o gerador escreve, o parser le de volta com integridade OK.
    public void Parser_le_de_volta_o_que_o_gerador_escreveu()
    {
        var linhas = new[]
        {
            new LinhaMarcacaoAfd(Nsr.De(1), CpfA, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), TipoRep.RepC),
            new LinhaMarcacaoAfd(Nsr.De(2), CpfB, new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), TipoRep.RepC),
            new LinhaMarcacaoAfd(Nsr.De(3), CpfA, new DateTimeOffset(2026, 6, 1, 13, 0, 0, TimeSpan.Zero), TipoRep.RepC),
        };
        var bytes = GeradorAfd.Gerar(Cabecalho(), linhas);

        var resultado = _parser.Parse(bytes);

        resultado.IntegridadeOk.Should().BeTrue();
        resultado.CrcOk.Should().BeTrue();
        resultado.NsrContinuo.Should().BeTrue();
        resultado.ContadorTrailerOk.Should().BeTrue();
        resultado.Marcacoes.Should().HaveCount(3);
        resultado.PrimeiroNsr.Should().Be(1);
        resultado.UltimoNsr.Should().Be(3);
        resultado.Marcacoes[0].Cpf.Digitos.Should().Be(CpfA.Digitos);
        resultado.Marcacoes[0].DataHora.Hour.Should().Be(8);
        resultado.Cabecalho.CnpjDigitos.Should().Be(CnpjEnte.Digitos);
    }

    [Fact] // REP-P emite marcacao tipo 7; o parser le e devolve origem RepP.
    public void Parser_distingue_marcacao_rep_p_tipo_7()
    {
        var linhas = new[]
        {
            new LinhaMarcacaoAfd(Nsr.De(1), CpfA, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), TipoRep.RepP),
        };
        var texto = Encoding.Latin1.GetString(GeradorAfd.Gerar(Cabecalho(), linhas));

        // O registro de marcacao de REP-P comeca com o tipo 7.
        texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Single(l => l.StartsWith('7'))
            .Should().NotBeNull();

        var resultado = _parser.Parse(Encoding.Latin1.GetBytes(texto));
        resultado.Marcacoes.Single().Origem.Should().Be(TipoRep.RepP);
    }

    [Fact] // CRC adulterado -> CrcOk falso (integridade reprovada).
    public void Parser_detecta_crc_adulterado()
    {
        var linhas = new[]
        {
            new LinhaMarcacaoAfd(Nsr.De(1), CpfA, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), TipoRep.RepC),
        };
        var texto = Encoding.Latin1.GetString(GeradorAfd.Gerar(Cabecalho(), linhas));

        // Altera o CPF de uma marcacao SEM recalcular o CRC do trailer -> CRC nao confere.
        var adulterado = texto.Replace(CpfA.Digitos, CpfB.Digitos, StringComparison.Ordinal);

        var resultado = _parser.Parse(Encoding.Latin1.GetBytes(adulterado));
        resultado.CrcOk.Should().BeFalse();
        resultado.IntegridadeOk.Should().BeFalse();
    }

    [Fact] // Lacuna de NSR (1,2,4) -> NsrContinuo falso (sinal de supressao, auditavel).
    public void Parser_detecta_lacuna_de_nsr()
    {
        var linhas = new[]
        {
            new LinhaMarcacaoAfd(Nsr.De(1), CpfA, new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero), TipoRep.RepC),
            new LinhaMarcacaoAfd(Nsr.De(2), CpfA, new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), TipoRep.RepC),
            new LinhaMarcacaoAfd(Nsr.De(4), CpfA, new DateTimeOffset(2026, 6, 1, 13, 0, 0, TimeSpan.Zero), TipoRep.RepC),
        };
        var bytes = GeradorAfd.Gerar(Cabecalho(), linhas);

        var resultado = _parser.Parse(bytes);

        resultado.NsrContinuo.Should().BeFalse();
        resultado.CrcOk.Should().BeTrue(); // o arquivo e integro; apenas a sequencia tem lacuna.
        resultado.IntegridadeOk.Should().BeFalse();
    }

    [Fact] // Arquivo sem trailer/cabecalho -> FormatException (fail-closed na estrutura).
    public void Parser_recusa_arquivo_malformado()
    {
        var lixo = Encoding.Latin1.GetBytes("conteudo qualquer sem estrutura\r\n");

        var acao = () => _parser.Parse(lixo);

        acao.Should().Throw<FormatException>();
    }
}
