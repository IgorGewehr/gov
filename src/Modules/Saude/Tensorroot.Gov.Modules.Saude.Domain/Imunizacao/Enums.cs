namespace Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;

/// <summary>
/// Tipo de dose no esquema vacinal do Programa Nacional de Imunizacoes (PNI). Modela as etapas do
/// esquema (doses da serie primaria, reforcos e dose unica), governando o aprazamento da proxima.
/// </summary>
public enum TipoDose
{
    /// <summary>Dose unica (esquema de uma so dose).</summary>
    Unica = 0,

    /// <summary>Primeira dose (D1) da serie primaria.</summary>
    Primeira = 1,

    /// <summary>Segunda dose (D2).</summary>
    Segunda = 2,

    /// <summary>Terceira dose (D3).</summary>
    Terceira = 3,

    /// <summary>Primeiro reforco (R1).</summary>
    PrimeiroReforco = 11,

    /// <summary>Segundo reforco (R2).</summary>
    SegundoReforco = 12,

    /// <summary>Dose anual (ex.: influenza).</summary>
    Anual = 20,
}

/// <summary>Situacao vacinal consolidada do paciente para um imunobiologico/esquema.</summary>
public enum SituacaoVacinal
{
    /// <summary>Esquema completo (todas as doses previstas aplicadas).</summary>
    Completo = 1,

    /// <summary>Esquema em andamento (ha proxima dose aprazada no futuro).</summary>
    EmDia = 2,

    /// <summary>Ha dose aprazada vencida (atrasada) — alvo de busca ativa.</summary>
    Atrasado = 3,
}
