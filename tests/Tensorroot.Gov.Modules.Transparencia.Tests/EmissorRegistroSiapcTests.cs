using System.Text;
using FluentAssertions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Cobertura do emissor posicional SIAPC/PAD: largura fixa, encoding ISO-8859-1, padding/alinhamento por
/// tipo (Caractere/Numerico/Valor/Data), sinal de valor e a convenção do nome do ZIP.
/// </summary>
public sealed class EmissorRegistroSiapcTests
{
    private static RegistroLeiauteDef RegistroExemplo()
        => RegistroLeiauteDef.Definir(
            "10",
            "EXEMPLO.TXT",
            [
                CampoLeiaute.Definir("Texto", 1, 5, TipoCampoLeiaute.Caractere, true),
                CampoLeiaute.Definir("Numero", 6, 4, TipoCampoLeiaute.Numerico, true),
                CampoLeiaute.Definir("Valor", 10, 8, TipoCampoLeiaute.Valor, true),
                CampoLeiaute.Definir("Data", 18, 8, TipoCampoLeiaute.Data, true),
            ]);

    private static Dictionary<string, ValorCampo> Valores(
        string texto, long numero, decimal valor, DateOnly data)
        => new()
        {
            ["Texto"] = ValorCampo.Caractere("Texto", texto),
            ["Numero"] = ValorCampo.Numerico("Numero", numero),
            ["Valor"] = ValorCampo.Monetario("Valor", valor),
            ["Data"] = ValorCampo.DataCampo("Data", data),
        };

    [Fact] // Largura fixa: a linha tem exatamente a soma das larguras dos campos.
    public void Linha_tem_largura_fixa_da_grade()
    {
        var registro = RegistroExemplo();

        var linha = EmissorRegistroSiapc.SerializarLinha(
            registro, Valores("AB", 7, 12.34m, new DateOnly(2026, 3, 1)));

        linha.Length.Should().Be(registro.LarguraLinha).And.Be(25);
    }

    [Fact] // Caractere: alinhado a ESQUERDA, completado com ESPACOS a direita.
    public void Caractere_alinha_a_esquerda_com_espacos()
    {
        var registro = RegistroExemplo();

        var linha = EmissorRegistroSiapc.SerializarLinha(
            registro, Valores("AB", 7, 12.34m, new DateOnly(2026, 3, 1)));

        linha[..5].Should().Be("AB   ");
    }

    [Fact] // Numerico: alinhado a DIREITA, completado com ZEROS a esquerda.
    public void Numerico_alinha_a_direita_com_zeros()
    {
        var registro = RegistroExemplo();

        var linha = EmissorRegistroSiapc.SerializarLinha(
            registro, Valores("AB", 7, 12.34m, new DateOnly(2026, 3, 1)));

        linha.Substring(5, 4).Should().Be("0007");
    }

    [Fact] // Valor: 1a posicao = sinal, restante = CENTAVOS com zeros a esquerda (positivo).
    public void Valor_positivo_tem_sinal_mais_e_centavos()
    {
        var registro = RegistroExemplo();

        var linha = EmissorRegistroSiapc.SerializarLinha(
            registro, Valores("AB", 7, 12.34m, new DateOnly(2026, 3, 1)));

        // 8 posicoes: '+' + 7 digitos => 12.34 => 1234 centavos => "+0001234".
        linha.Substring(9, 8).Should().Be("+0001234");
    }

    [Fact] // Valor negativo carrega sinal '-'.
    public void Valor_negativo_tem_sinal_menos()
    {
        var registro = RegistroExemplo();

        var linha = EmissorRegistroSiapc.SerializarLinha(
            registro, Valores("AB", 7, -5.00m, new DateOnly(2026, 3, 1)));

        linha.Substring(9, 8).Should().Be("-0000500");
    }

    [Fact] // Data: formato ddmmaaaa.
    public void Data_em_ddmmaaaa()
    {
        var registro = RegistroExemplo();

        var linha = EmissorRegistroSiapc.SerializarLinha(
            registro, Valores("AB", 7, 12.34m, new DateOnly(2026, 3, 9)));

        linha.Substring(17, 8).Should().Be("09032026");
    }

    [Fact] // Encoding ISO-8859-1: 1 byte por caractere Latin-1 (acento ocupa 1 byte, nao 2 como UTF-8).
    public void Encoding_e_iso_8859_1_um_byte_por_caractere()
    {
        var registro = RegistroLeiauteDef.Definir(
            "10", "ACENTO.TXT", [CampoLeiaute.Definir("Texto", 1, 6, TipoCampoLeiaute.Caractere, true)]);

        var linha = EmissorRegistroSiapc.SerializarLinha(
            registro,
            new Dictionary<string, ValorCampo> { ["Texto"] = ValorCampo.Caractere("Texto", "AÇÃO") });

        var bytes = EmissorRegistroSiapc.EncodingSiapc.GetBytes(linha);
        bytes.Length.Should().Be(6, "ISO-8859-1 usa 1 byte por caractere (largura fixa em bytes = colunas)");
        bytes[1].Should().Be(0xC7, "'Ç' = 0xC7 em ISO-8859-1");
        // Round-trip confirma que nao houve corrupcao de encoding.
        EmissorRegistroSiapc.EncodingSiapc.GetString(bytes).Should().Be("AÇÃO  ");
    }

    [Fact] // Numerico que nao cabe na largura lanca overflow (nao trunca digito significativo).
    public void Numerico_que_excede_largura_lanca()
    {
        var registro = RegistroLeiauteDef.Definir(
            "10", "NUM.TXT", [CampoLeiaute.Definir("Numero", 1, 3, TipoCampoLeiaute.Numerico, true)]);

        var acao = () => EmissorRegistroSiapc.SerializarLinha(
            registro,
            new Dictionary<string, ValorCampo> { ["Numero"] = ValorCampo.Numerico("Numero", 12345) });

        acao.Should().Throw<OverflowException>();
    }

    [Fact] // Corpo: cada linha termina com CR/LF.
    public void Corpo_usa_terminador_crlf()
    {
        var registro = RegistroExemplo();
        var linhas = new List<IReadOnlyDictionary<string, ValorCampo>>
        {
            Valores("AB", 7, 12.34m, new DateOnly(2026, 3, 1)),
        };

        var bytes = EmissorRegistroSiapc.SerializarCorpo(registro, linhas);
        var texto = EmissorRegistroSiapc.EncodingSiapc.GetString(bytes);

        texto.Should().EndWith("\r\n");
        texto.Length.Should().Be(25 + 2);
    }

    [Fact] // Convencao do nome do ZIP: CNPJ.DataIni.DataFim.DataGer.Tipo.CodRemessa.zip.
    public void Nome_zip_segue_convencao()
    {
        var nome = NomeArquivoRemessaSiapc.Compor(
            "99999999000199",
            new DateOnly(2007, 1, 1),
            new DateOnly(2007, 5, 31),
            new DateOnly(2007, 6, 15),
            'P',
            10);

        nome.Should().Be("99999999000199.01012007.31052007.15062007.P.000000000010.zip");
    }

    [Fact] // CNPJ invalido (sem 14 digitos) e rejeitado na composicao do nome.
    public void Nome_zip_rejeita_cnpj_invalido()
    {
        var acao = () => NomeArquivoRemessaSiapc.Compor(
            "123", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 1), 'P', 1);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // Grade com buraco/sobreposicao e rejeitada (largura fixa determinística).
    public void Grade_com_buraco_e_rejeitada()
    {
        var acao = () => RegistroLeiauteDef.Definir(
            "10",
            "BURACO.TXT",
            [
                CampoLeiaute.Definir("A", 1, 3, TipoCampoLeiaute.Caractere),
                CampoLeiaute.Definir("B", 6, 3, TipoCampoLeiaute.Caractere), // deveria iniciar em 4.
            ]);

        acao.Should().Throw<InvalidOperationException>();
    }
}
