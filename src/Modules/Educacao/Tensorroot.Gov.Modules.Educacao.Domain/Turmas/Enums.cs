namespace Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

/// <summary>Etapa/modalidade de ensino da turma (mapeamento BNCC fica para fase posterior).</summary>
public enum Etapa
{
    /// <summary>Educacao Infantil (creche/pre-escola).</summary>
    EducacaoInfantil = 1,

    /// <summary>Ensino Fundamental — anos iniciais (1º ao 5º ano).</summary>
    Fundamental1 = 2,

    /// <summary>Ensino Fundamental — anos finais (6º ao 9º ano).</summary>
    Fundamental2 = 3,

    /// <summary>Ensino Medio.</summary>
    EnsinoMedio = 4,

    /// <summary>Educacao de Jovens e Adultos.</summary>
    Eja = 5,

    /// <summary>Educacao Especial.</summary>
    EducacaoEspecial = 6,
}

/// <summary>Turno de funcionamento da turma.</summary>
public enum Turno
{
    /// <summary>Matutino.</summary>
    Matutino = 1,

    /// <summary>Vespertino.</summary>
    Vespertino = 2,

    /// <summary>Noturno.</summary>
    Noturno = 3,

    /// <summary>Integral.</summary>
    Integral = 4,
}

/// <summary>Situacao (estado) da turma no ciclo de operacao do ano letivo.</summary>
public enum SituacaoTurma
{
    /// <summary>Planejada (criada, ainda nao habilitada a enturmar).</summary>
    Planejada = 1,

    /// <summary>Aberta (habilitada a enturmacao/matricula).</summary>
    Aberta = 2,

    /// <summary>Encerrada (terminal — fim do ano letivo ou turma sem matricula).</summary>
    Encerrada = 3,
}
