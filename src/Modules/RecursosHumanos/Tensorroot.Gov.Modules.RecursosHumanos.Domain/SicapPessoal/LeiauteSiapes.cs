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

    // Linha de ato ("Formato Basico" do leiaute, campos posicionais ate a data de nascimento — nucleo
    // PoC do leiaute 57). Datas DDMMAAAA; texto a esquerda; numerico a direita preenchido com zeros.
    private static string MontarLinhaAto(RemessaSicapPessoal remessa, AtoAdmissaoSicap ato)
    {
        var construtor = new StringBuilder();
        construtor.Append(TextoEsquerda(ato.IdentificadorAto, 50));                                       // 01 IDENTIFICADOR_ATO (50)
        construtor.Append(CodigosSiapes.Codigo(MovimentoSiapes.Insercao));                                 // 02 CD_MOVIMENTO (1) — "I"
        construtor.Append(NumericoEsquerda(remessa.CodigoOrgao, 6));                                       // 03 CD_ORGAO (6)
        construtor.Append(NumericoEsquerda((int)ato.TipoAto, 2));                                          // 04 CD_TIPO_ATO (2)
        construtor.Append(ato.Regime.Codigo());                                                            // 05 CD_REGIME_JURIDICO (1 util)
        construtor.Append(ato.MotivoExtincao is { } motivo ? NumericoEsquerda((int)motivo, 2) : Brancos(2)); // 07 CD_EXTINCAO (2)
        construtor.Append(DataOuBrancos(ato.DataHistorica));                                               // 10 DATA_HISTORICA (8)
        construtor.Append(DataOuBrancos(ato.DataTermino));                                                 // 11 DATA_TERMINO (8)
        construtor.Append(ato.DataAto.ToString(FormatoData, CultureInfo.InvariantCulture));                // 12 DATA_ATO (8)
        construtor.Append(NumericoEsquerda(ato.CargaHorariaSemanal, 3));                                   // 13 CARGA_HORARIA (3)
        construtor.Append(ato.ClassificacaoConcurso is { } cls ? NumericoEsquerda(cls, 5) : Brancos(5));   // 14 CLASSIFICACAO (5)
        construtor.Append(TextoEsquerda(ato.DescricaoCargo, 35));                                          // 16 DS_CARGO (35)
        construtor.Append(TextoEsquerda(ato.Nome, 70));                                                    // 21 NOME (70)
        construtor.Append(TextoEsquerda(Numeros(ato.Cpf), 15));                                            // 23 CPF (15)
        construtor.Append(ato.DataNascimento.ToString(FormatoData, CultureInfo.InvariantCulture));         // 25 DATA_NASCIMENTO (8)
        return construtor.ToString();
    }

    private static string DataOuBrancos(DateOnly? data)
        => data is { } d ? d.ToString(FormatoData, CultureInfo.InvariantCulture) : Brancos(8);

    private static string TextoEsquerda(string valor, int tamanho)
    {
        var texto = valor.Length > tamanho ? valor[..tamanho] : valor;
        return texto.PadRight(tamanho);
    }

    private static string NumericoEsquerda(int valor, int tamanho)
        => valor.ToString(CultureInfo.InvariantCulture).PadLeft(tamanho, '0')[^tamanho..];

    private static string Brancos(int tamanho) => new(' ', tamanho);

    private static string Numeros(string valor) => new(valor.Where(char.IsDigit).ToArray());
}
