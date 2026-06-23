namespace Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

/// <summary>Sexo do aluno (conforme tabela do EducaCenso/INEP).</summary>
public enum Sexo
{
    /// <summary>Sexo feminino.</summary>
    Feminino = 1,

    /// <summary>Sexo masculino.</summary>
    Masculino = 2,

    /// <summary>Nao informado/ignorado.</summary>
    Ignorado = 9,
}

/// <summary>Situacao (estado) do cadastro do aluno na rede de ensino.</summary>
public enum SituacaoAluno
{
    /// <summary>Aluno ativo (estado inicial apos o cadastro).</summary>
    Ativo = 1,

    /// <summary>Aluno transferido para outra rede (terminal).</summary>
    Transferido = 2,

    /// <summary>Cadastro inativado (obito, evasao definitiva, duplicidade) — terminal.</summary>
    Inativo = 3,
}

/// <summary>Grau de parentesco/vinculo do responsavel com o aluno (EducaCenso/LGPD art. 14).</summary>
public enum Parentesco
{
    /// <summary>Mae.</summary>
    Mae = 1,

    /// <summary>Pai.</summary>
    Pai = 2,

    /// <summary>Avo (materno/paterno).</summary>
    Avo = 3,

    /// <summary>Tutor/guardiao legal.</summary>
    Tutor = 4,

    /// <summary>Outro responsavel.</summary>
    Outro = 9,
}
