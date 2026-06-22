namespace Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

/// <summary>Classificacao de risco/urgencia da solicitacao de regulacao; ordena a fila de regulacao.</summary>
public enum Prioridade
{
    /// <summary>Sem urgencia (fila eletiva).</summary>
    Eletiva = 1,

    /// <summary>Prioridade clinica intermediaria.</summary>
    Prioritaria = 2,

    /// <summary>Urgencia.</summary>
    Urgente = 3,

    /// <summary>Emergencia (maxima prioridade).</summary>
    Emergencia = 4,
}

/// <summary>Situacao (estado) da solicitacao de regulacao no fluxo.</summary>
public enum SituacaoSolicitacaoRegulacao
{
    /// <summary>Aguardando regulacao (estado inicial).</summary>
    Solicitada = 1,

    /// <summary>Deferida; vaga reservada no SISREG, cota consumida.</summary>
    Autorizada = 2,

    /// <summary>Indeferida pelo regulador — terminal.</summary>
    Negada = 3,

    /// <summary>Devolvida ao solicitante para complementacao.</summary>
    Devolvida = 4,

    /// <summary>Procedimento realizado — terminal.</summary>
    Executada = 5,

    /// <summary>Cancelada pelo solicitante — terminal.</summary>
    Cancelada = 6,
}
