using System.Globalization;

namespace Tensorroot.Gov.Integracoes.Cnab;

/// <summary>Dados da empresa/ente pagador para a remessa CNAB.</summary>
/// <param name="Cnpj">CNPJ do ente (com ou sem máscara).</param>
/// <param name="Nome">Nome do ente.</param>
/// <param name="CodigoBanco">Código do banco (ex.: 001 BB, 104 Caixa).</param>
/// <param name="Agencia">Agência (sem dígito).</param>
/// <param name="Conta">Conta (sem dígito).</param>
/// <param name="DigitoConta">Dígito da conta.</param>
public sealed record EmpresaCnab(string Cnpj, string Nome, int CodigoBanco, string Agencia, string Conta, string DigitoConta);

/// <summary>Uma ordem de pagamento (crédito em conta — Segmento A).</summary>
/// <param name="FavorecidoNome">Nome do favorecido.</param>
/// <param name="FavorecidoDocumento">CPF/CNPJ do favorecido.</param>
/// <param name="BancoFavorecido">Código do banco do favorecido.</param>
/// <param name="AgenciaFavorecido">Agência do favorecido.</param>
/// <param name="ContaFavorecido">Conta do favorecido.</param>
/// <param name="Valor">Valor do pagamento.</param>
/// <param name="DataPagamento">Data de pagamento.</param>
public sealed record PagamentoCnab(
    string FavorecidoNome,
    string FavorecidoDocumento,
    int BancoFavorecido,
    string AgenciaFavorecido,
    string ContaFavorecido,
    decimal Valor,
    DateOnly DataPagamento);

/// <summary>Gera arquivos de remessa bancária CNAB 240 (FEBRABAN).</summary>
public interface IGeradorRemessaCnab
{
    /// <summary>Gera o conteúdo do arquivo de remessa de pagamentos.</summary>
    /// <param name="empresa">Ente pagador.</param>
    /// <param name="pagamentos">Pagamentos a incluir.</param>
    /// <param name="numeroRemessa">Número sequencial da remessa.</param>
    /// <param name="geracao">Data/hora de geração.</param>
    /// <returns>Conteúdo do arquivo (linhas separadas por CRLF).</returns>
    string GerarRemessaPagamento(EmpresaCnab empresa, IReadOnlyList<PagamentoCnab> pagamentos, int numeroRemessa, DateTime geracao);
}

/// <summary>
/// Gerador de arquivo de remessa <b>CNAB 240</b> (padrão FEBRABAN) para pagamentos a
/// fornecedores/credores (crédito em conta — Segmento A). Linhas de 240 posições de largura
/// fixa. Layouts específicos de banco podem exigir ajustes pontuais sobre esta base.
/// </summary>
public sealed class Cnab240Gerador : IGeradorRemessaCnab
{
    /// <summary>Largura fixa de cada registro CNAB 240.</summary>
    public const int LarguraLinha = 240;

    /// <summary>Gera o conteúdo do arquivo de remessa de pagamentos.</summary>
    /// <param name="empresa">Ente pagador.</param>
    /// <param name="pagamentos">Pagamentos a incluir.</param>
    /// <param name="numeroRemessa">Número sequencial da remessa.</param>
    /// <param name="geracao">Data/hora de geração.</param>
    /// <returns>Conteúdo do arquivo (linhas separadas por CRLF).</returns>
    public string GerarRemessaPagamento(
        EmpresaCnab empresa,
        IReadOnlyList<PagamentoCnab> pagamentos,
        int numeroRemessa,
        DateTime geracao)
    {
        ArgumentNullException.ThrowIfNull(empresa);
        ArgumentNullException.ThrowIfNull(pagamentos);

        var linhas = new List<string>(pagamentos.Count + 4)
        {
            HeaderArquivo(empresa, numeroRemessa, geracao),
            HeaderLote(empresa),
        };

        var sequencial = 0;
        decimal total = 0m;
        foreach (var pagamento in pagamentos)
        {
            linhas.Add(SegmentoA(empresa, pagamento, ++sequencial));
            total += pagamento.Valor;
        }

        linhas.Add(TrailerLote(pagamentos.Count, total));
        linhas.Add(TrailerArquivo(pagamentos.Count));

        return string.Join("\r\n", linhas) + "\r\n";
    }

    private static string HeaderArquivo(EmpresaCnab empresa, int numeroRemessa, DateTime geracao)
        => new Linha()
            .Num(1, 3, empresa.CodigoBanco)          // banco
            .Num(4, 4, 0)                            // lote (0000 = header arquivo)
            .Num(8, 1, 0)                            // tipo de registro
            .Num(18, 1, 2)                           // tipo de inscrição (2 = CNPJ)
            .Num(19, 14, Digitos(empresa.Cnpj))      // CNPJ
            .Num(53, 5, Parse(empresa.Agencia))      // agência
            .Num(59, 12, Parse(empresa.Conta))       // conta
            .Num(71, 1, Parse(empresa.DigitoConta))  // dígito
            .Alfa(73, 30, empresa.Nome)              // nome do ente
            .Alfa(103, 30, "TENSORROOT.GOV")         // nome do banco/origem
            .Num(143, 1, 1)                          // código remessa (1 = remessa)
            .Num(144, 8, long.Parse(geracao.ToString("ddMMyyyy", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture))
            .Num(158, 6, numeroRemessa)              // número sequencial do arquivo
            .Num(164, 3, 103)                        // versão do layout
            .ToString();

    private static string HeaderLote(EmpresaCnab empresa)
        => new Linha()
            .Num(1, 3, empresa.CodigoBanco)
            .Num(4, 4, 1)                            // lote 0001
            .Num(8, 1, 1)                            // tipo de registro
            .Alfa(9, 1, "C")                         // tipo de operação (C = crédito)
            .Num(10, 2, 20)                          // tipo de serviço (20 = pagamento a fornecedor)
            .Num(12, 2, 1)                           // forma de lançamento (01 = crédito em conta)
            .Num(18, 1, 2)                           // tipo de inscrição (CNPJ)
            .Num(19, 14, Digitos(empresa.Cnpj))
            .Alfa(73, 30, empresa.Nome)
            .ToString();

    private static string SegmentoA(EmpresaCnab empresa, PagamentoCnab pagamento, int sequencial)
        => new Linha()
            .Num(1, 3, empresa.CodigoBanco)
            .Num(4, 4, 1)
            .Num(8, 1, 3)                            // tipo de registro (detalhe)
            .Num(9, 5, sequencial)                   // número sequencial do registro no lote
            .Alfa(14, 1, "A")                        // segmento
            .Num(21, 3, pagamento.BancoFavorecido)   // banco favorecido
            .Num(24, 5, Parse(pagamento.AgenciaFavorecido))
            .Num(30, 12, Parse(pagamento.ContaFavorecido))
            .Alfa(44, 30, pagamento.FavorecidoNome)
            .Num(94, 8, long.Parse(pagamento.DataPagamento.ToString("ddMMyyyy", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture))
            .Num(120, 15, Centavos(pagamento.Valor)) // valor do pagamento (em centavos)
            .ToString();

    private static string TrailerLote(int quantidade, decimal total)
        => new Linha()
            .Num(1, 3, 0)
            .Num(4, 4, 1)
            .Num(8, 1, 5)                            // tipo de registro (trailer de lote)
            .Num(18, 6, quantidade + 2)              // quantidade de registros do lote
            .Num(24, 18, Centavos(total))            // somatório dos valores
            .ToString();

    private static string TrailerArquivo(int quantidadePagamentos)
        => new Linha()
            .Num(4, 4, 9999)                         // lote 9999
            .Num(8, 1, 9)                            // tipo de registro (trailer de arquivo)
            .Num(18, 6, 1)                           // quantidade de lotes
            .Num(24, 6, quantidadePagamentos + 4)    // quantidade total de registros do arquivo
            .ToString();

    private static long Centavos(decimal valor) => (long)decimal.Round(valor * 100m, 0, MidpointRounding.AwayFromZero);

    private static long Digitos(string valor)
    {
        long resultado = 0;
        foreach (var caractere in valor)
        {
            if (char.IsAsciiDigit(caractere))
            {
                resultado = (resultado * 10) + (caractere - '0');
            }
        }

        return resultado;
    }

    private static long Parse(string valor) => Digitos(valor);

    /// <summary>Construtor fluente de uma linha CNAB de 240 posições (largura fixa, base 1).</summary>
    private sealed class Linha
    {
        private readonly char[] _buffer = new char[LarguraLinha];

        public Linha() => Array.Fill(_buffer, ' ');

        public Linha Num(int posicao, int tamanho, long valor)
        {
            var texto = valor.ToString(CultureInfo.InvariantCulture);
            var campo = texto.Length >= tamanho ? texto[^tamanho..] : texto.PadLeft(tamanho, '0');
            campo.AsSpan().CopyTo(_buffer.AsSpan(posicao - 1, tamanho));
            return this;
        }

        public Linha Alfa(int posicao, int tamanho, string valor)
        {
            var texto = (valor ?? string.Empty).ToUpperInvariant();
            var campo = texto.Length >= tamanho ? texto[..tamanho] : texto.PadRight(tamanho);
            campo.AsSpan().CopyTo(_buffer.AsSpan(posicao - 1, tamanho));
            return this;
        }

        public override string ToString() => new(_buffer);
    }
}
