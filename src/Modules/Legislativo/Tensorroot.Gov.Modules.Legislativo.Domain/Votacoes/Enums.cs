namespace Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

/// <summary>Modalidade de apuracao da votacao.</summary>
public enum TipoVotacao
{
    /// <summary>Apuracao simbolica (placar agregado, sem registro individual).</summary>
    Simbolica = 1,

    /// <summary>Apuracao nominal (registro individual por vereador).</summary>
    Nominal = 2,

    /// <summary>Apuracao secreta (sigilo do voto individual).</summary>
    Secreta = 3,
}

/// <summary>Criterio de aprovacao exigido pela especie da materia (CF/88 art. 29).</summary>
public enum MaioriaExigida
{
    /// <summary>Maioria simples: maior que 50% dos presentes (lei ordinaria).</summary>
    Simples = 1,

    /// <summary>Maioria absoluta: maior que 50% dos membros (LC, derrubada de veto, Regimento).</summary>
    Absoluta = 2,

    /// <summary>Maioria qualificada: 2/3 em dois turnos (Emenda a LOM).</summary>
    Qualificada = 3,
}

/// <summary>Direcao (sentido) do voto de um vereador.</summary>
public enum SentidoVoto
{
    /// <summary>Voto favoravel.</summary>
    Sim = 1,

    /// <summary>Voto contrario.</summary>
    Nao = 2,

    /// <summary>Abstencao.</summary>
    Abstencao = 3,
}

/// <summary>Situacao (estado) da votacao na maquina de estados.</summary>
public enum SituacaoVotacao
{
    /// <summary>Aberta para registro de votos (estado inicial).</summary>
    Aberta = 1,

    /// <summary>Encerrada e apurada (terminal).</summary>
    Encerrada = 2,

    /// <summary>Cancelada (terminal).</summary>
    Cancelada = 3,
}

/// <summary>Desfecho apurado da votacao.</summary>
public enum ResultadoVotacao
{
    /// <summary>Materia aprovada (maioria exigida atingida).</summary>
    Aprovado = 1,

    /// <summary>Materia rejeitada (maioria nao atingida).</summary>
    Rejeitado = 2,
}
