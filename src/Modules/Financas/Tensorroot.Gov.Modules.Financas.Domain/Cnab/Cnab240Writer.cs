using System.Globalization;
using System.Text;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;

namespace Tensorroot.Gov.Modules.Financas.Domain.Cnab;

/// <summary>
/// Gerador do arquivo de remessa CNAB240 (FEBRABAN) de pagamento a fornecedores/servidores. Emite os
/// registros posicionais de 240 posições: Header de Arquivo (0), Header de Lote (1), Segmentos A e B (3),
/// Trailer de Lote (5) e Trailer de Arquivo (9). Cada linha tem exatamente 240 caracteres (CRLF entre
/// registros). Posições conforme o Layout Padrão FEBRABAN CNAB240 (Tipo de Serviço 20 — pagamento a
/// fornecedores). A transmissão real ao banco é diferida (M10).
/// </summary>
public static class Cnab240Writer
{
    private const int TamanhoRegistro = 240;
    private const string TipoServicoFornecedores = "20"; // pagamento a fornecedores
    private const string VersaoLayoutArquivo = "103";
    private const string VersaoLayoutLote = "046";

    /// <summary>Gera o conteúdo textual do arquivo de remessa CNAB240.</summary>
    /// <param name="remessa">Dados da remessa.</param>
    /// <returns>Texto do arquivo (registros de 240 posições separados por CRLF).</returns>
    /// <exception cref="RemessaCnabInvalidaException">Se a remessa não tiver favorecidos.</exception>
    public static string Gerar(RemessaCnab240 remessa)
    {
        ArgumentNullException.ThrowIfNull(remessa);
        if (remessa.Favorecidos.Count == 0)
        {
            throw new RemessaCnabInvalidaException("Remessa CNAB240 sem favorecidos.");
        }

        var linhas = new List<string>
        {
            HeaderArquivo(remessa),
            HeaderLote(remessa),
        };

        var sequencial = 0;
        foreach (var favorecido in remessa.Favorecidos)
        {
            sequencial++;
            linhas.Add(SegmentoA(remessa, favorecido, sequencial));
            sequencial++;
            linhas.Add(SegmentoB(remessa, favorecido, sequencial));
        }

        // Trailer de lote: quantidade = header lote + detalhes (A+B) + trailer lote.
        var quantidadeRegistrosLote = 2 + (remessa.Favorecidos.Count * 2) + 1;
        var somatorio = remessa.Favorecidos.Sum(f => f.Valor);
        linhas.Add(TrailerLote(remessa, quantidadeRegistrosLote, somatorio));

        // Trailer de arquivo: quantidade de registros = todas as linhas + o próprio trailer de arquivo.
        var quantidadeRegistrosArquivo = linhas.Count + 1;
        linhas.Add(TrailerArquivo(remessa, quantidadeLotes: 1, quantidadeRegistrosArquivo));

        return string.Join("\r\n", linhas.Select(Validar));
    }

    private static string HeaderArquivo(RemessaCnab240 r)
    {
        var p = r.Pagador;
        var sb = new StringBuilder(TamanhoRegistro);
        sb.Append(CampoCnab.Numerico(p.CodigoBanco, 3));            // 1-3 banco
        sb.Append(CampoCnab.Numerico("0", 4));                       // 4-7 lote 0000
        sb.Append('0');                                              // 8 tipo registro
        sb.Append(CampoCnab.Brancos(9));                            // 9-17 brancos (CNAB)
        sb.Append(CampoCnab.Numerico(((int)p.TipoInscricao).ToString(CultureInfo.InvariantCulture), 1)); // 18 tipo inscricao
        sb.Append(CampoCnab.Numerico(p.NumeroInscricao, 14));      // 19-32 inscricao
        sb.Append(CampoCnab.Alfanumerico(p.Convenio, 20));         // 33-52 convenio
        sb.Append(CampoCnab.Numerico(p.Agencia, 5));               // 53-57 agencia
        sb.Append(CampoCnab.Alfanumerico(p.DvAgencia, 1));         // 58 dv agencia
        sb.Append(CampoCnab.Numerico(p.Conta, 12));               // 59-70 conta
        sb.Append(CampoCnab.Alfanumerico(p.DvConta, 1));          // 71 dv conta
        sb.Append(CampoCnab.Alfanumerico(p.DvAgenciaConta, 1));   // 72 dv ag/conta
        sb.Append(CampoCnab.Alfanumerico(p.NomeEmpresa, 30));     // 73-102 nome empresa
        sb.Append(CampoCnab.Alfanumerico("TENSORROOT.GOV", 30));  // 103-132 nome banco/origem
        sb.Append(CampoCnab.Brancos(10));                          // 133-142 brancos
        sb.Append('1');                                            // 143 codigo remessa (1=remessa)
        sb.Append(CampoCnab.Data(r.DataGeracao));                 // 144-151 data geracao
        sb.Append(CampoCnab.Hora(r.HoraGeracao));                 // 152-157 hora geracao
        sb.Append(CampoCnab.Numerico(r.SequencialArquivo, 6));    // 158-163 sequencial arquivo
        sb.Append(CampoCnab.Numerico(VersaoLayoutArquivo, 3));    // 164-166 versao layout
        sb.Append(CampoCnab.Numerico("0", 5));                    // 167-171 densidade
        sb.Append(CampoCnab.Brancos(TamanhoRegistro - sb.Length)); // resto reservado/brancos
        return sb.ToString();
    }

    private static string HeaderLote(RemessaCnab240 r)
    {
        var p = r.Pagador;
        var sb = new StringBuilder(TamanhoRegistro);
        sb.Append(CampoCnab.Numerico(p.CodigoBanco, 3));            // 1-3 banco
        sb.Append(CampoCnab.Numerico("1", 4));                       // 4-7 lote 0001
        sb.Append('1');                                              // 8 tipo registro
        sb.Append('C');                                             // 9 tipo operacao (C=credito)
        sb.Append(TipoServicoFornecedores);                        // 10-11 tipo servico
        sb.Append(CampoCnab.Numerico(((int)r.FormaLancamento).ToString(CultureInfo.InvariantCulture), 2)); // 12-13 forma lancamento
        sb.Append(CampoCnab.Numerico(VersaoLayoutLote, 3));       // 14-16 versao layout lote
        sb.Append(CampoCnab.Brancos(1));                           // 17 cnab
        sb.Append(CampoCnab.Numerico(((int)p.TipoInscricao).ToString(CultureInfo.InvariantCulture), 1)); // 18 tipo inscricao
        sb.Append(CampoCnab.Numerico(p.NumeroInscricao, 14));      // 19-32 inscricao
        sb.Append(CampoCnab.Alfanumerico(p.Convenio, 20));         // 33-52 convenio
        sb.Append(CampoCnab.Numerico(p.Agencia, 5));               // 53-57 agencia
        sb.Append(CampoCnab.Alfanumerico(p.DvAgencia, 1));         // 58 dv agencia
        sb.Append(CampoCnab.Numerico(p.Conta, 12));               // 59-70 conta
        sb.Append(CampoCnab.Alfanumerico(p.DvConta, 1));          // 71 dv conta
        sb.Append(CampoCnab.Alfanumerico(p.DvAgenciaConta, 1));   // 72 dv ag/conta
        sb.Append(CampoCnab.Alfanumerico(p.NomeEmpresa, 30));     // 73-102 nome empresa
        sb.Append(CampoCnab.Brancos(TamanhoRegistro - sb.Length)); // mensagem/endereco/resto
        return sb.ToString();
    }

    private static string SegmentoA(RemessaCnab240 r, FavorecidoCnab f, int sequencial)
    {
        var sb = new StringBuilder(TamanhoRegistro);
        sb.Append(CampoCnab.Numerico(r.Pagador.CodigoBanco, 3));    // 1-3 banco
        sb.Append(CampoCnab.Numerico("1", 4));                       // 4-7 lote
        sb.Append('3');                                              // 8 tipo registro
        sb.Append(CampoCnab.Numerico(sequencial, 5));               // 9-13 sequencial
        sb.Append('A');                                             // 14 segmento
        sb.Append('0');                                            // 15 tipo movimento (0=inclusao)
        sb.Append(CampoCnab.Numerico("0", 2));                    // 16-17 codigo instrucao
        sb.Append(CampoCnab.Numerico("0", 3));                    // 18-20 camara
        sb.Append(CampoCnab.Numerico(f.CodigoBancoFavorecido, 3)); // 21-23 banco favorecido
        sb.Append(CampoCnab.Numerico(f.AgenciaFavorecido, 5));     // 24-28 agencia favorecido
        sb.Append(CampoCnab.Alfanumerico(f.DvAgenciaFavorecido, 1)); // 29 dv agencia
        sb.Append(CampoCnab.Numerico(f.ContaFavorecido, 12));      // 30-41 conta favorecido
        sb.Append(CampoCnab.Alfanumerico(f.DvContaFavorecido, 1)); // 42 dv conta
        sb.Append(CampoCnab.Brancos(1));                           // 43 dv ag/conta
        sb.Append(CampoCnab.Alfanumerico(f.NomeFavorecido, 30));   // 44-73 nome favorecido
        sb.Append(CampoCnab.Alfanumerico(f.NumeroDocumento, 20));  // 74-93 numero documento
        sb.Append(CampoCnab.Data(f.DataPagamento));               // 94-101 data pagamento
        sb.Append(CampoCnab.Alfanumerico("BRL", 3));              // 102-104 tipo moeda
        sb.Append(CampoCnab.Zeros(15));                           // 105-119 quantidade moeda
        sb.Append(CampoCnab.Valor(f.Valor, 15));                  // 120-134 valor pagamento
        sb.Append(CampoCnab.Brancos(20));                         // 135-154 nosso numero
        sb.Append(CampoCnab.Zeros(8));                            // 155-162 data real efetivacao
        sb.Append(CampoCnab.Zeros(15));                          // 163-177 valor real efetivacao
        sb.Append(CampoCnab.Brancos(TamanhoRegistro - sb.Length)); // resto
        return sb.ToString();
    }

    private static string SegmentoB(RemessaCnab240 r, FavorecidoCnab f, int sequencial)
    {
        var sb = new StringBuilder(TamanhoRegistro);
        sb.Append(CampoCnab.Numerico(r.Pagador.CodigoBanco, 3));    // 1-3 banco
        sb.Append(CampoCnab.Numerico("1", 4));                       // 4-7 lote
        sb.Append('3');                                              // 8 tipo registro
        sb.Append(CampoCnab.Numerico(sequencial, 5));               // 9-13 sequencial
        sb.Append('B');                                             // 14 segmento
        sb.Append(CampoCnab.Brancos(3));                           // 15-17 cnab
        sb.Append(CampoCnab.Numerico(((int)f.TipoInscricaoFavorecido).ToString(CultureInfo.InvariantCulture), 1)); // 18 tipo inscricao
        sb.Append(CampoCnab.Numerico(f.NumeroInscricaoFavorecido, 14)); // 19-32 inscricao favorecido
        // Quando PIX, a chave vai no campo de logradouro/identificação (uso bancário); caso contrário, brancos.
        if (r.FormaLancamento == FormaLancamentoCnab.PixTransferencia && !string.IsNullOrWhiteSpace(f.ChavePix))
        {
            sb.Append(CampoCnab.Alfanumerico(f.ChavePix, 99));     // 33-131 endereco/chave PIX (uso bancario)
        }
        else
        {
            sb.Append(CampoCnab.Brancos(99));                      // 33-131 endereco
        }

        sb.Append(CampoCnab.Data(f.DataPagamento));               // 132-139 data vencimento (=pagamento)
        sb.Append(CampoCnab.Valor(f.Valor, 15));                  // 140-154 valor documento
        sb.Append(CampoCnab.Brancos(TamanhoRegistro - sb.Length)); // abatimento/desconto/mora/multa/resto
        return sb.ToString();
    }

    private static string TrailerLote(RemessaCnab240 r, int quantidadeRegistros, decimal somatorio)
    {
        var sb = new StringBuilder(TamanhoRegistro);
        sb.Append(CampoCnab.Numerico(r.Pagador.CodigoBanco, 3));    // 1-3 banco
        sb.Append(CampoCnab.Numerico("1", 4));                       // 4-7 lote
        sb.Append('5');                                              // 8 tipo registro
        sb.Append(CampoCnab.Brancos(9));                           // 9-17 cnab
        sb.Append(CampoCnab.Numerico(quantidadeRegistros, 6));     // 18-23 quantidade registros
        sb.Append(CampoCnab.Valor(somatorio, 18));                // 24-41 somatoria valores
        sb.Append(CampoCnab.Zeros(18));                            // 42-59 somatoria quantidade moeda
        sb.Append(CampoCnab.Numerico("0", 6));                    // 60-65 numero aviso debito
        sb.Append(CampoCnab.Brancos(TamanhoRegistro - sb.Length)); // resto
        return sb.ToString();
    }

    private static string TrailerArquivo(RemessaCnab240 r, int quantidadeLotes, int quantidadeRegistros)
    {
        var sb = new StringBuilder(TamanhoRegistro);
        sb.Append(CampoCnab.Numerico(r.Pagador.CodigoBanco, 3));    // 1-3 banco
        sb.Append(CampoCnab.Numerico("9999", 4));                  // 4-7 lote 9999
        sb.Append('9');                                              // 8 tipo registro
        sb.Append(CampoCnab.Brancos(9));                           // 9-17 cnab
        sb.Append(CampoCnab.Numerico(quantidadeLotes, 6));        // 18-23 quantidade lotes
        sb.Append(CampoCnab.Numerico(quantidadeRegistros, 6));    // 24-29 quantidade registros
        sb.Append(CampoCnab.Numerico("0", 6));                    // 30-35 quantidade contas concil.
        sb.Append(CampoCnab.Brancos(TamanhoRegistro - sb.Length)); // resto
        return sb.ToString();
    }

    private static string Validar(string linha)
    {
        if (linha.Length != TamanhoRegistro)
        {
            throw new RemessaCnabInvalidaException($"Registro CNAB240 com {linha.Length} posicoes (esperado {TamanhoRegistro}).");
        }

        return linha;
    }
}
