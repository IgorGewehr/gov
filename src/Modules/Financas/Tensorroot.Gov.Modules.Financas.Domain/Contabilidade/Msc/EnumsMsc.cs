namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Msc;

/// <summary>
/// Tipo de valor de uma linha da Matriz de Saldos Contabeis (taxonomia XBRL GL). Cada conta analitica
/// emite registros por tipo de valor numa competencia. [validar-oficial] mapeamento ao leiaute MSC
/// (Anexo II Portaria STN 642/2019): beginning_balance / period_change / ending_balance.
/// </summary>
public enum TipoValorMsc
{
    /// <summary>Saldo inicial do periodo (XBRL GL <c>beginning_balance</c>) — do <c>SaldoAnterior</c>.</summary>
    SaldoInicial = 1,

    /// <summary>Movimento do periodo (XBRL GL <c>period_change</c>) — debitos/creditos do periodo.</summary>
    MovimentoPeriodo = 2,

    /// <summary>Saldo final do periodo (XBRL GL <c>ending_balance</c>) — do <c>SaldoAtual</c>.</summary>
    SaldoFinal = 3,
}

/// <summary>
/// Tipo da Matriz de Saldos Contabeis quanto a competencia. A geracao de Encerramento (mes 13, contas de
/// resultado zeradas apos apuracao) depende da rotina de encerramento do exercicio — M3.x/M4.
/// </summary>
public enum TipoMatrizMsc
{
    /// <summary>MSC Agregada mensal (mes 1-12).</summary>
    Agregada = 1,

    /// <summary>MSC de Encerramento (mes 13, anual). // TODO(validar-oficial) — M3.x.</summary>
    Encerramento = 2,
}
