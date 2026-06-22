namespace Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;

/// <summary>Fase de uso da palavra na sessao (momentos regimentais de fala).</summary>
public enum FaseUsoPalavra
{
    /// <summary>Pequeno Expediente.</summary>
    PequenoExpediente = 1,

    /// <summary>Grande Expediente.</summary>
    GrandeExpediente = 2,

    /// <summary>Explicacao Pessoal.</summary>
    ExplicacaoPessoal = 3,

    /// <summary>Tribuna Livre.</summary>
    TribunaLivre = 4,
}

/// <summary>Situacao de uma inscricao de orador na tribuna.</summary>
public enum SituacaoInscricao
{
    /// <summary>Inscrito (aguardando a chamada da presidencia).</summary>
    Inscrito = 1,

    /// <summary>Em uso da palavra (cronometro correndo).</summary>
    EmUso = 2,

    /// <summary>Fala concluida.</summary>
    Concluido = 3,

    /// <summary>Inscricao cancelada (antes de falar).</summary>
    Cancelado = 4,
}
