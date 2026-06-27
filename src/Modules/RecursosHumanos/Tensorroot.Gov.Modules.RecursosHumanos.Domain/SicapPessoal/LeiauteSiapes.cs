using System.Globalization;
using System.Text;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

/// <summary>
/// Gerador PURO do arquivo de importacao SIAPES (leiaute estadual do TCE-RS — auditoria de admissoes).
/// Monta o CABECALHO (Tabela 13: CD_ORGAO, DT_INICIAL_MOV, NRO_MOV, somatorios por movimento, VERSAO),
/// o CORPO (uma linha por ato, campos posicionais do "Formato Basico") e o FINALIZADOR (contagem de
/// registros). Centraliza o leiaute para reuso pelo caso de uso de geracao; nao acessa I/O. A
/// transmissao real ao SIAPESweb fica no M10 (ACL + A1). Datas no formato DDMMAAAA (item 10/12).
/// </summary>
public static class LeiauteSiapes
{
    private const string FormatoData = "ddMMyyyy";

    /// <summary>Gera o conteudo textual do arquivo de importacao de uma remessa (cabecalho + atos + finalizador).</summary>
    /// <param name="remessa">Remessa (gerada) a serializar.</param>
    /// <returns>Conteudo do arquivo, linhas separadas por <c>\r\n</c>.</returns>
    /// <exception cref="ArgumentNullException">Se a remessa for nula.</exception>
    public static string Gerar(RemessaSicapPessoal remessa)
    {
        ArgumentNullException.ThrowIfNull(remessa);

        var construtor = new StringBuilder();
        construtor.Append(MontarCabecalho(remessa)).Append("\r\n");
        foreach (var ato in remessa.Atos)
        {
            construtor.Append(MontarLinhaAto(remessa, ato)).Append("\r\n");
        }

        // FINALIZADOR: total de registros gravados (cabecalho + corpo), numerico, 10 bytes.
        var totalRegistros = remessa.Atos.Count + 1;
        construtor.Append(NumericoEsquerda(totalRegistros, 10));
        return construtor.ToString();
    }

    // Cabecalho (Tabela 13). No escopo PoC todos os atos sao INSERCAO ("I"): QTD_INCLUSAO = QTD_MOVIMENTOS.
    private static string MontarCabecalho(RemessaSicapPessoal remessa)
    {
        var quantidade = remessa.Atos.Count;
        var construtor = new StringBuilder();
        construtor.Append(NumericoEsquerda(remessa.CodigoOrgao, 6));                 // 01 CD_ORGAO (6)
        construtor.Append(remessa.DataGeracaoLote.ToString(FormatoData, CultureInfo.InvariantCulture)); // 02 DT_INICIAL_MOV (8)
        construtor.Append(NumericoEsquerda(remessa.SequencialLote, 10));             // 03 NRO_MOV (10)
        construtor.Append(NumericoEsquerda(quantidade, 10));                          // 04 QTD_MOVIMENTOS (10)
        construtor.Append(NumericoEsquerda(quantidade, 10));                          // 05 QTD_INCLUSAO "I" (10)
        construtor.Append(NumericoEsquerda(0, 10));                                   // 06 QTD_EXCLUSAO "D" (10)
        construtor.Append(NumericoEsquerda(0, 10));                                   // 07 QTD_ALT_ATO "U" (10)
        construtor.Append(NumericoEsquerda(0, 10));                                   // 08 QTD_ALT_CONCURSO "C" (10)
        construtor.Append(NumericoEsquerda(0, 10));                                   // 09 QTD_ALT_PESSOA "P" (10)
        construtor.Append(NumericoEsquerda(0, 10));                                   // 10 QTD_ALT_FUNDAMENTACAO "F" (10)
        construtor.Append(NumericoEsquerda(0, 10));                                   // 11 QTD_ALT_NOME "A" (10)
        construtor.Append(NumericoEsquerda(0, 10));                                   // 12 QTD_DEL_NOME "E" (10)
        construtor.Append(NumericoEsquerda(remessa.VersaoLeiaute, 2));                // 13 VERSAO (2)
        return construtor.ToString();
    }

    // Linha de ato — "Formato Basico" do SIAPES (TCE-RS, Tabela 14 do leiaute "Consist57"). O arquivo e de
    // LARGURA FIXA por posicao: TODOS os campos 01..25 do nucleo sao emitidos NA ORDEM e LARGURA EXATAS do
    // leiaute oficial, ainda que vazios — campos posicionais intermediarios omitidos deslocam todos os
    // offsets seguintes e o arquivo e rejeitado por desalinhamento (correcao P1-7 da AUDITORIA-FINAL).
    // Convencao oficial: alfanumerico/caracter a esquerda preenchido com brancos; data DDMMAAAA (brancos se
    // ausente); numerico a direita com zeros quando se aplica e e informado, BRANCOS quando o campo nao se
    // aplica ao titulo/movimento. Posicoes (Inicio-Fim): 01[1-50] 02[51] 03[52-57] 04[58-59] 05[60-61]
    // 06[62-63] 07[64-65] 08[66-68] 09[69-74] 10[75-82] 11[83-90] 12[91-98] 13[99-101] 14[102-106] 15[107]
    // 16[108-142] 17[143-212] 18[213-247] 19[248-317] 20[318-367] 21[368-437] 22[438-452] 23[453-467]
    // 24[468-482] 25[483-490]. // TODO(M10): campos 26..57 (concurso/fundamentacao/disciplina) e tabelas de
    // dominio proprias do TCE-RS; o nucleo 01..25 cobre o ato de admissao do PoC.
    private static string MontarLinhaAto(RemessaSicapPessoal remessa, AtoAdmissaoSicap ato)
    {
        var construtor = new StringBuilder();
        construtor.Append(TextoEsquerda(ato.IdentificadorAto, 50));                                       // 01 IDENTIFICADOR_ATO  (C,50)
        construtor.Append(CodigosSiapes.Codigo(MovimentoSiapes.Insercao));                                 // 02 CD_MOVIMENTO       (C,1) "I"
        construtor.Append(NumericoEsquerda(remessa.CodigoOrgao, 6));                                       // 03 CD_ORGAO           (N,6)
        construtor.Append(NumericoEsquerda((int)ato.TipoAto, 2));                                          // 04 CD_TIPO_ATO        (N,2)
        construtor.Append(NumericoEsquerda((int)ato.Regime, 2));                                           // 05 CD_REGIME_JURIDICO (N,2)
        construtor.Append(Brancos(2));                                                                     // 06 CD_REGIME_JURIDICO_ANT (N,2) n/a PoC
        construtor.Append(ato.MotivoExtincao is { } motivo ? NumericoEsquerda((int)motivo, 2) : Brancos(2)); // 07 CD_EXTINCAO     (N,2)
        construtor.Append(Brancos(3));                                                                     // 08 CD_MUNICIPIO_MAE  (N,3) n/a PoC
        construtor.Append(Brancos(6));                                                                     // 09 CD_ORGAO_ANT      (N,6) n/a PoC
        construtor.Append(DataOuBrancos(ato.DataHistorica));                                               // 10 DATA_HISTORICA    (D,8)
        construtor.Append(DataOuBrancos(ato.DataTermino));                                                 // 11 DATA_TERMINO       (D,8)
        construtor.Append(ato.DataAto.ToString(FormatoData, CultureInfo.InvariantCulture));                // 12 DATA_ATO           (D,8)
        construtor.Append(NumericoEsquerda(ato.CargaHorariaSemanal, 3));                                   // 13 CARGA_HORARIA      (N,3)
        construtor.Append(ato.ClassificacaoConcurso is { } cls ? NumericoEsquerda(cls, 5) : Brancos(5));   // 14 CLASSIFICACAO      (N,5)
        construtor.Append(Brancos(1));                                                                     // 15 CONCURSADO (S/N)   (C,1) n/a PoC
        construtor.Append(TextoEsquerda(ato.DescricaoCargo, 35));                                          // 16 DS_CARGO           (C,35)
        construtor.Append(Brancos(70));                                                                    // 17 ESPECIALIZACAO_PROF (C,70) n/a PoC
        construtor.Append(Brancos(35));                                                                    // 18 DS_CARGO_ANTERIOR  (C,35) n/a PoC
        construtor.Append(Brancos(70));                                                                    // 19 ESPECIALIZACAO_ANT (C,70) n/a PoC
        construtor.Append(TextoEsquerda(IdentificadorPessoa(ato), 50));                                    // 20 IDENTIFICADOR_PESSOA (C,50)
        construtor.Append(TextoEsquerda(ato.Nome, 70));                                                    // 21 NOME              (C,70)
        construtor.Append(Brancos(15));                                                                    // 22 RG                (C,15) n/a PoC
        construtor.Append(TextoEsquerda(Numeros(ato.Cpf), 15));                                            // 23 CPF               (C,15)
        construtor.Append(Brancos(15));                                                                    // 24 TITULO_ELEITOR    (C,15) n/a PoC
        construtor.Append(ato.DataNascimento.ToString(FormatoData, CultureInfo.InvariantCulture));         // 25 DATA_NASCIMENTO    (D,8)
        return construtor.ToString();
    }

    // IDENTIFICADOR_PESSOA (campo 20): chave unica do servidor no sistema originador. Usa o ServidorId
    // interno (GUID) quando o ato tem vinculo de rastreio; senao recai no identificador do ato (matricula),
    // garantindo o campo nao-nulo exigido para os movimentos I/D/U/P/A.
    private static string IdentificadorPessoa(AtoAdmissaoSicap ato)
        => ato.ServidorId is { } servidorId
            ? servidorId.Value.ToString()
            : ato.IdentificadorAto;

    private static string DataOuBrancos(DateOnly? data)
        => data is { } d ? d.ToString(FormatoData, CultureInfo.InvariantCulture) : Brancos(8);

    private static string TextoEsquerda(string valor, int tamanho)
    {
        var texto = valor.Length > tamanho ? valor[..tamanho] : valor;
        return texto.PadRight(tamanho);
    }

    // Numerico do leiaute: alinhado a direita, preenchido com zeros. FAIL-CLOSED contra overflow/sinal
    // (correcao P2-6): se o valor nao couber na largura, truncar pegaria os digitos INFERIORES e emitiria
    // um codigo ERRADO (ex.: outro orgao) silenciosamente; negativo corromperia o campo com o sinal "-".
    // Rejeita-se a geracao do arquivo — desalinhamento/codigo trocado e rejeicao garantida pelo TCE-RS.
    private static string NumericoEsquerda(int valor, int tamanho)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        var digitos = valor.ToString(CultureInfo.InvariantCulture);
        if (digitos.Length > tamanho)
        {
            throw new InvalidOperationException(
                $"Valor numerico {valor} excede a largura de {tamanho} posicoes do campo SIAPES (overflow do leiaute posicional).");
        }

        return digitos.PadLeft(tamanho, '0');
    }

    private static string Brancos(int tamanho) => new(' ', tamanho);

    private static string Numeros(string valor) => new(valor.Where(char.IsDigit).ToArray());
}
