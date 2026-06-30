namespace Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

/// <summary>Situação (estado) da Dívida Ativa.</summary>
public enum SituacaoDividaAtiva
{
    /// <summary>Inscrita em Dívida Ativa.</summary>
    Inscrita = 1,

    /// <summary>Com Certidão de Dívida Ativa (CDA) emitida.</summary>
    CdaEmitida = 2,

    /// <summary>Protestada em cartório.</summary>
    Protestada = 3,

    /// <summary>Em execução fiscal (Lei 6.830/80).</summary>
    EmExecucaoFiscal = 4,

    /// <summary>Parcelada (REFIS) — exigibilidade suspensa.</summary>
    Parcelada = 5,

    /// <summary>Quitada.</summary>
    Quitada = 6,

    /// <summary>Cancelada.</summary>
    Cancelada = 7,

    /// <summary>
    /// Execução fiscal SUSPENSA por 1 ano (LEF art. 40, caput/§1º): devedor não localizado ou sem bens
    /// penhoráveis. Durante este ano a prescrição (intercorrente) NÃO corre. [revisao-humana-juridica]
    /// </summary>
    ExecucaoSuspensa = 8,

    /// <summary>
    /// Execução fiscal ARQUIVADA (LEF art. 40, §2º): findo o ano de suspensão, corre a prescrição
    /// INTERCORRENTE quinquenal (§4º; Súmula 314/STJ). [revisao-humana-juridica]
    /// </summary>
    ExecucaoArquivada = 9,
}

/// <summary>
/// Causa de SUSPENSÃO DA EXIGIBILIDADE do crédito tributário (CTN art. 151) — overlay ORTOGONAL ao
/// ciclo de cobrança (<see cref="SituacaoDividaAtiva"/>): pode incidir sobre dívida inscrita, com CDA
/// ou protestada e cessar depois. [revisao-humana-juridica]
/// </summary>
public enum CausaSuspensaoExigibilidade
{
    /// <summary>Moratória (CTN art. 151, I).</summary>
    Moratoria = 1,

    /// <summary>Depósito do montante integral (CTN art. 151, II; Súmula 112/STJ — integral e em dinheiro).</summary>
    DepositoMontanteIntegral = 2,

    /// <summary>Reclamações e recursos no processo administrativo fiscal (CTN art. 151, III).</summary>
    ReclamacaoRecursoAdministrativo = 3,

    /// <summary>Liminar em mandado de segurança (CTN art. 151, IV).</summary>
    LiminarMandadoSeguranca = 4,

    /// <summary>Liminar/tutela antecipada em outras ações (CTN art. 151, V).</summary>
    TutelaAntecipadaOutrasAcoes = 5,

    /// <summary>Parcelamento (CTN art. 151, VI) — também REINICIA o termo prescricional (art. 174 p.ú. IV).</summary>
    Parcelamento = 6,
}
