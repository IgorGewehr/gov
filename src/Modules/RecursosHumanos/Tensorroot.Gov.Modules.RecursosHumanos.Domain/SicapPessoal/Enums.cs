namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

/// <summary>
/// Codigo do tipo do ato de admissao (campo CD_TIPO_ATO / "Titulos 01 a 15" do leiaute SIAPES do
/// TCE-RS — Tabela 4). O valor numerico do enum E' o codigo oficial transmitido. Fonte: leiaute
/// estadual 57 posicoes (FONTES-NORMATIVAS.md B6).
/// </summary>
public enum TipoAtoAdmissao
{
    /// <summary>01 - Admissao por concurso publico.</summary>
    ConcursoPublico = 1,

    /// <summary>02 - Contratacao por prazo determinado (temporario).</summary>
    PrazoDeterminado = 2,

    /// <summary>03 - Admissao sem fundamentacao legal.</summary>
    SemFundamentacao = 3,

    /// <summary>04 - Admissao decorrente de decisao judicial.</summary>
    DecisaoJudicial = 4,

    /// <summary>05 - Reenquadramento.</summary>
    Reenquadramento = 5,

    /// <summary>06 - Transferencia de municipio-mae.</summary>
    TransferenciaMunicipioMae = 6,

    /// <summary>07 - Transposicao de regime juridico de trabalho.</summary>
    TransposicaoRegime = 7,

    /// <summary>08 - Transferencia de orgao.</summary>
    TransferenciaOrgao = 8,

    /// <summary>09 - Readaptacao.</summary>
    Readaptacao = 9,

    /// <summary>10 - Readmissao.</summary>
    Readmissao = 10,

    /// <summary>11 - Reconducao.</summary>
    Reconducao = 11,

    /// <summary>12 - Reintegracao.</summary>
    Reintegracao = 12,

    /// <summary>13 - Nomeacao decorrente de concurso interno.</summary>
    ConcursoInterno = 13,

    /// <summary>14 - Aproveitamento.</summary>
    Aproveitamento = 14,

    /// <summary>15 - Outros.</summary>
    Outros = 15,
}

/// <summary>
/// Codigo do regime juridico de trabalho (campo CD_REGIME_JURIDICO — Tabela 5 do leiaute SIAPES). O
/// caractere transmitido e' obtido por <see cref="CodigosSiapes.Codigo(RegimeJuridicoSiapes)"/>.
/// </summary>
public enum RegimeJuridicoSiapes
{
    /// <summary>C - Celetista.</summary>
    Celetista = 1,

    /// <summary>E - Estatutario.</summary>
    Estatutario = 2,

    /// <summary>A - Administrativo.</summary>
    Administrativo = 3,
}

/// <summary>
/// Codigo da extincao de vinculo (campo CD_EXTINCAO — Tabela 6 do leiaute SIAPES). O valor numerico do
/// enum E' o codigo oficial; aplicavel apenas quando ha data de termino (vinculo extinto).
/// </summary>
public enum MotivoExtincaoVinculo
{
    /// <summary>01 - Inativacao INSS.</summary>
    InativacaoInss = 1,

    /// <summary>02 - Inativacao regime previdenciario proprio.</summary>
    InativacaoRpps = 2,

    /// <summary>03 - Falecimento.</summary>
    Falecimento = 3,

    /// <summary>04 - Termino de contrato.</summary>
    TerminoContrato = 4,

    /// <summary>05 - Exoneracao, demissao ou rescisao.</summary>
    ExoneracaoDemissaoRescisao = 5,

    /// <summary>06 - Nova investidura em cargo ou emprego.</summary>
    NovaInvestidura = 6,

    /// <summary>07 - Anulacao do ato.</summary>
    AnulacaoAto = 7,

    /// <summary>08 - Outros.</summary>
    Outros = 8,
}

/// <summary>
/// Codigo da operacao do movimento (campo CD_MOVIMENTO do leiaute SIAPES). O caractere transmitido e'
/// obtido por <see cref="CodigosSiapes.Codigo(MovimentoSiapes)"/>. No escopo PoC operamos a INSERCAO de ato
/// ("I"); os demais movimentos (D/U/C/P/F/A) ficam disponiveis no dominio para evolucao.
/// </summary>
public enum MovimentoSiapes
{
    /// <summary>I - Insercao de ato.</summary>
    Insercao = 1,

    /// <summary>D - Exclusao de ato.</summary>
    Exclusao = 2,

    /// <summary>U - Alteracao de ato.</summary>
    Alteracao = 3,

    /// <summary>C - Alteracao de concurso.</summary>
    AlteracaoConcurso = 4,

    /// <summary>P - Alteracao de pessoa.</summary>
    AlteracaoPessoa = 5,

    /// <summary>F - Alteracao de fundamentacao legal.</summary>
    AlteracaoFundamentacao = 6,

    /// <summary>A - Alteracao de nome proprio do servidor.</summary>
    AlteracaoNome = 7,
}

/// <summary>Situacao (ciclo de vida) de uma remessa de pessoal SICAP-AP/SIAPES.</summary>
public enum SituacaoRemessaSicap
{
    /// <summary>Remessa montada/aberta com seus atos (estado inicial); pode ser gerada/fechada.</summary>
    Aberta = 1,

    /// <summary>Arquivo de importacao gerado e fechado (pronto para transmissao — M10).</summary>
    Gerada = 2,

    /// <summary>Transmitida ao TCE-RS (// TODO(M10): transmissao real ao SIAPESweb).</summary>
    Transmitida = 3,
}

/// <summary>Conversoes dos enums SIAPES para o caractere oficial do leiaute.</summary>
public static class CodigosSiapes
{
    /// <summary>Caractere oficial do regime juridico (Tabela 5: C/E/A).</summary>
    /// <param name="regime">Regime juridico.</param>
    /// <returns>Caractere do leiaute.</returns>
    public static char Codigo(this RegimeJuridicoSiapes regime) => regime switch
    {
        RegimeJuridicoSiapes.Celetista => 'C',
        RegimeJuridicoSiapes.Estatutario => 'E',
        RegimeJuridicoSiapes.Administrativo => 'A',
        _ => throw new ArgumentOutOfRangeException(nameof(regime), regime, "Regime juridico SIAPES invalido."),
    };

    /// <summary>Caractere oficial do movimento (CD_MOVIMENTO: I/D/U/C/P/F/A).</summary>
    /// <param name="movimento">Tipo de movimento.</param>
    /// <returns>Caractere do leiaute.</returns>
    public static char Codigo(this MovimentoSiapes movimento) => movimento switch
    {
        MovimentoSiapes.Insercao => 'I',
        MovimentoSiapes.Exclusao => 'D',
        MovimentoSiapes.Alteracao => 'U',
        MovimentoSiapes.AlteracaoConcurso => 'C',
        MovimentoSiapes.AlteracaoPessoa => 'P',
        MovimentoSiapes.AlteracaoFundamentacao => 'F',
        MovimentoSiapes.AlteracaoNome => 'A',
        _ => throw new ArgumentOutOfRangeException(nameof(movimento), movimento, "Movimento SIAPES invalido."),
    };
}
