namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

/// <summary>
/// Tipo de exame medico ocupacional (campo <c>tpExameOcup</c> do grupo <c>exMedOcup</c> do S-2220).
/// Espelha o dominio do leiaute S-1.3 (MOS). // TODO(validar-oficial): dominio exato no XSD travado.
/// </summary>
public enum TipoExameOcupacional
{
    /// <summary>Admissional (0).</summary>
    Admissional = 0,

    /// <summary>Periodico (1).</summary>
    Periodico = 1,

    /// <summary>Retorno ao trabalho (2).</summary>
    RetornoAoTrabalho = 2,

    /// <summary>Mudanca de funcao/risco (3).</summary>
    MudancaDeRiscoOcupacional = 3,

    /// <summary>Monitoracao pontual/avaliacao (4) — uso conforme orientacao do MOS.</summary>
    MonitoracaoPontual = 4,

    /// <summary>Demissional (9).</summary>
    Demissional = 9,
}

/// <summary>
/// Resultado/aptidao do ASO (campo <c>resAso</c> do grupo <c>aso</c> do S-2220).
/// // TODO(validar-oficial): dominio exato no XSD travado (tipicamente 1=Apto, 2=Inapto).
/// </summary>
public enum ResultadoAso
{
    /// <summary>Apto (1).</summary>
    Apto = 1,

    /// <summary>Inapto (2).</summary>
    Inapto = 2,
}

/// <summary>
/// Situacao geradora do acidente/doenca (campo <c>tpAcid</c>/<c>tpCat</c> do grupo <c>cat</c> do S-2210).
/// // TODO(validar-oficial): dominio exato no XSD travado.
/// </summary>
public enum TipoCat
{
    /// <summary>CAT inicial (1).</summary>
    Inicial = 1,

    /// <summary>CAT de reabertura (2).</summary>
    Reabertura = 2,

    /// <summary>CAT de comunicacao de obito (3).</summary>
    ComunicacaoObito = 3,
}

/// <summary>
/// Tipo de acidente (campo <c>tpAcid</c> do grupo <c>cat</c> do S-2210).
/// // TODO(validar-oficial): dominio exato no XSD travado (Tabela 25).
/// </summary>
public enum TipoAcidente
{
    /// <summary>Acidente tipico (1).</summary>
    Tipico = 1,

    /// <summary>Doenca ocupacional (2).</summary>
    Doenca = 2,

    /// <summary>Acidente de trajeto (3).</summary>
    Trajeto = 3,
}

/// <summary>
/// Situacao (estado) de um registro de SST na maquina de vida do registro (nao confundir com a maquina
/// de estados do <c>EventoESocial</c>): <c>Registrado</c> (vigente) ou <c>Cancelado</c> (tornado sem efeito).
/// </summary>
public enum SituacaoRegistroSst
{
    /// <summary>Registro vigente.</summary>
    Registrado = 1,

    /// <summary>Registro cancelado (sem efeito; estado terminal).</summary>
    Cancelado = 2,
}
