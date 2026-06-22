namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

/// <summary>
/// Tipo de servico socioassistencial ofertado no acompanhamento familiar
/// (Tipificacao Nacional dos Servicos Socioassistenciais — Res. CNAS 109/2009).
/// </summary>
public enum TipoServico
{
    /// <summary>Protecao e Atendimento Integral a Familia (PAIF) — ofertado somente em CRAS.</summary>
    Paif = 1,

    /// <summary>Protecao e Atendimento Especializado a Familias e Individuos (PAEFI) — ofertado somente em CREAS.</summary>
    Paefi = 2,

    /// <summary>Servico de Convivencia e Fortalecimento de Vinculos (SCFV).</summary>
    Scfv = 3,
}

/// <summary>
/// Tipo da unidade de atendimento responsavel pelo prontuario; define os servicos
/// admissiveis (PAIF so em CRAS; PAEFI so em CREAS).
/// </summary>
public enum TipoUnidadeAtendimento
{
    /// <summary>Centro de Referencia de Assistencia Social (protecao social basica).</summary>
    Cras = 1,

    /// <summary>Centro de Referencia Especializado de Assistencia Social (protecao social especial).</summary>
    Creas = 2,

    /// <summary>Centro de Referencia Especializado para Populacao em Situacao de Rua (Centro POP).</summary>
    CentroPop = 3,
}

/// <summary>Situacao (estado) do prontuario no ciclo de acompanhamento familiar.</summary>
public enum SituacaoProntuario
{
    /// <summary>Prontuario aberto; acompanhamento em curso (estado inicial).</summary>
    Aberto = 1,

    /// <summary>Acompanhamento encerrado, com motivo (terminal).</summary>
    Encerrado = 2,
}

/// <summary>Tipo de violacao de direito identificada no acompanhamento (dado sensivel — art. 11 LGPD).</summary>
public enum TipoViolacaoDireito
{
    /// <summary>Violencia fisica.</summary>
    ViolenciaFisica = 1,

    /// <summary>Violencia psicologica.</summary>
    ViolenciaPsicologica = 2,

    /// <summary>Violencia sexual.</summary>
    ViolenciaSexual = 3,

    /// <summary>Negligencia ou abandono.</summary>
    Negligencia = 4,

    /// <summary>Trabalho infantil.</summary>
    TrabalhoInfantil = 5,

    /// <summary>Situacao de rua.</summary>
    SituacaoDeRua = 6,

    /// <summary>Discriminacao por raca, etnia, genero ou orientacao sexual.</summary>
    Discriminacao = 7,

    /// <summary>Outras violacoes de direitos.</summary>
    Outra = 99,
}
