namespace Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

/// <summary>Situacao (estado) da matricula no ciclo de vida do vinculo aluno-turma-escola.</summary>
public enum SituacaoMatricula
{
    /// <summary>Matricula ativa (estado inicial).</summary>
    Ativa = 1,

    /// <summary>Aluno transferido (terminal).</summary>
    Transferida = 2,

    /// <summary>Etapa/ano concluido (terminal).</summary>
    Concluida = 3,

    /// <summary>Abandono escolar (terminal).</summary>
    Abandono = 4,
}

/// <summary>Motivo de encerramento da matricula (encerramento por conclusao ou abandono).</summary>
public enum MotivoEncerramento
{
    /// <summary>Encerramento por conclusao da etapa/ano (leva a <see cref="SituacaoMatricula.Concluida"/>).</summary>
    Conclusao = 1,

    /// <summary>Encerramento por abandono escolar (leva a <see cref="SituacaoMatricula.Abandono"/>).</summary>
    Abandono = 2,
}

/// <summary>Rendimento do aluno (2a etapa do Censo — Situacao do Aluno).</summary>
public enum Rendimento
{
    /// <summary>Aluno aprovado.</summary>
    Aprovado = 1,

    /// <summary>Aluno reprovado.</summary>
    Reprovado = 2,
}

/// <summary>Movimento do aluno (2a etapa do Censo — Situacao do Aluno).</summary>
public enum Movimento
{
    /// <summary>Sem movimento (permaneceu na rede).</summary>
    SemMovimento = 1,

    /// <summary>Aluno transferido.</summary>
    Transferido = 2,

    /// <summary>Aluno em abandono.</summary>
    Abandono = 3,

    /// <summary>Aluno falecido.</summary>
    Falecido = 4,
}
