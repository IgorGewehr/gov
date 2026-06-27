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
}
