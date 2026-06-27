using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Regressao do leiaute SIAPES/SICAP-AP (TCE-RS, "Formato Basico" Tabela 14) — correcoes P1-6/P1-7/P2-6
/// da AUDITORIA-FINAL. O arquivo e de LARGURA FIXA por posicao: o nucleo 01..25 mede EXATAMENTE 490 bytes
/// e cada campo ocupa o offset oficial. Omitir campos posicionais intermediarios desloca todos os offsets
/// seguintes e o arquivo e rejeitado pelo Tribunal — por isso a prova ancora nas POSICOES, nao so no valor.
/// </summary>
public sealed class SicapPessoalLeiauteTests
{
    private static readonly Guid Tenant = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // O nucleo do ato (campos 01..25) tem largura fixa de 490 posicoes (vide mapa de offsets em LeiauteSiapes).
    private const int LarguraLinhaAto = 490;

    private static RemessaSicapPessoal RemessaComAtoConcursado(RegimeJuridicoSiapes regime)
    {
        var remessa = RemessaSicapPessoal.Abrir(Tenant, codigoOrgao: 12345, sequencialLote: 1, dataGeracaoLote: new DateOnly(2026, 6, 1));
        remessa.AdicionarAto(
            servidorId: null,
            identificadorAto: "MAT-0001",
            tipoAto: TipoAtoAdmissao.ConcursoPublico,
            regime: regime,
            cpf: "52998224725",
            nome: "Fulano de Tal",
            dataNascimento: new DateOnly(1990, 1, 2),
            descricaoCargo: "Analista",
            cargaHorariaSemanal: 40,
            classificacaoConcurso: 7,
            dataAto: new DateOnly(2026, 1, 5),
            dataHistorica: new DateOnly(2025, 12, 20),
            dataTermino: null,
            motivoExtincao: null);
        return remessa;
    }

    // Recorta o campo por posicao 1-based [inicio..fim] da LINHA DO ATO (2a linha do arquivo: cabecalho e a 1a).
    private static string Campo(string arquivo, int inicio, int fim)
    {
        var linhaAto = arquivo.Split("\r\n")[1];
        return linhaAto.Substring(inicio - 1, fim - inicio + 1);
    }

    [Fact] // P1-7: a linha do ato tem LARGURA FIXA de 490 posicoes (nucleo 01..25 completo, campos n/a em branco).
    public void Linha_do_ato_tem_largura_fixa_de_490_posicoes()
    {
        var arquivo = LeiauteSiapes.Gerar(RemessaComAtoConcursado(RegimeJuridicoSiapes.Estatutario));

        var linhaAto = arquivo.Split("\r\n")[1];
        linhaAto.Should().HaveLength(LarguraLinhaAto);
    }

    [Fact] // P1-7: cada campo do nucleo cai EXATAMENTE no seu offset oficial (sem deslocamento por campo omitido).
    public void Campos_caem_nos_offsets_oficiais()
    {
        var arquivo = LeiauteSiapes.Gerar(RemessaComAtoConcursado(RegimeJuridicoSiapes.Estatutario));

        Campo(arquivo, 1, 50).TrimEnd().Should().Be("MAT-0001");      // 01 IDENTIFICADOR_ATO
        Campo(arquivo, 51, 51).Should().Be("I");                       // 02 CD_MOVIMENTO (insercao)
        Campo(arquivo, 52, 57).Should().Be("012345");                  // 03 CD_ORGAO (6, zeros a esquerda)
        Campo(arquivo, 58, 59).Should().Be("01");                      // 04 CD_TIPO_ATO (concurso = 01)
        Campo(arquivo, 91, 98).Should().Be("05012026");                // 12 DATA_ATO (DDMMAAAA)
        Campo(arquivo, 99, 101).Should().Be("040");                    // 13 CARGA_HORARIA (40 -> 040)
        Campo(arquivo, 108, 142).TrimEnd().Should().Be("Analista");    // 16 DS_CARGO
        Campo(arquivo, 368, 437).TrimEnd().Should().Be("FULANO DE TAL"); // 21 NOME (maiusculas)
        Campo(arquivo, 453, 467).TrimEnd().Should().Be("52998224725"); // 23 CPF
        Campo(arquivo, 483, 490).Should().Be("02011990");              // 25 DATA_NASCIMENTO
    }

    [Theory] // P1-7: CD_REGIME_JURIDICO (campo 05, pos 60-61) e NUMERICO de 2 bytes — emite o valor do enum, nao C/E/A.
    [InlineData(RegimeJuridicoSiapes.Celetista, "01")]
    [InlineData(RegimeJuridicoSiapes.Estatutario, "02")]
    [InlineData(RegimeJuridicoSiapes.Administrativo, "03")]
    public void Regime_juridico_e_numerico_de_dois_bytes(RegimeJuridicoSiapes regime, string esperado)
    {
        var arquivo = LeiauteSiapes.Gerar(RemessaComAtoConcursado(regime));

        Campo(arquivo, 60, 61).Should().Be(esperado);
    }

    [Theory] // P1-6: o regime JURIDICO deriva do TIPO DO CARGO (atributo do vinculo), nao do regime previdenciario.
    [InlineData(TipoCargo.Efetivo, RegimeJuridicoSiapes.Estatutario)]
    [InlineData(TipoCargo.Comissionado, RegimeJuridicoSiapes.Administrativo)]
    [InlineData(TipoCargo.Temporario, RegimeJuridicoSiapes.Administrativo)]
    public void Regime_padrao_deriva_do_tipo_do_cargo(TipoCargo tipoCargo, RegimeJuridicoSiapes esperado)
    {
        MapeamentoSiapes.RegimePadraoDe(tipoCargo).Should().Be(esperado);
    }

    [Fact] // P2-6: fail-closed — codigo de orgao que estoura a largura do campo (6 bytes) NAO trunca silenciosamente.
    public void Codigo_de_orgao_alem_da_largura_e_rejeitado()
    {
        var remessa = RemessaSicapPessoal.Abrir(Tenant, codigoOrgao: 1234567, sequencialLote: 1, dataGeracaoLote: new DateOnly(2026, 6, 1));

        var gerar = () => LeiauteSiapes.Gerar(remessa);

        gerar.Should().Throw<InvalidOperationException>();
    }
}
