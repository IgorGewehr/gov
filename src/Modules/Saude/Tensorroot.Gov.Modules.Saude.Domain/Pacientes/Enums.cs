namespace Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

/// <summary>Situacao do cadastro do paciente no PEP/e-SUS APS.</summary>
public enum SituacaoPaciente
{
    /// <summary>Cadastro ativo (estado inicial apos a criacao).</summary>
    Ativo = 1,

    /// <summary>Cadastro inativado (obito, transferencia, duplicidade) — terminal.</summary>
    Inativo = 2,
}

/// <summary>Sexo do paciente (conforme tabela do CADSUS/e-SUS APS).</summary>
public enum Sexo
{
    /// <summary>Sexo feminino.</summary>
    Feminino = 1,

    /// <summary>Sexo masculino.</summary>
    Masculino = 2,

    /// <summary>Nao informado/ignorado.</summary>
    Ignorado = 9,
}
