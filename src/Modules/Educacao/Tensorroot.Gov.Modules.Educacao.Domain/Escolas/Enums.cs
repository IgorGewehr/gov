namespace Tensorroot.Gov.Modules.Educacao.Domain.Escolas;

/// <summary>Dependencia administrativa de uma escola (esfera mantenedora, leiaute do Censo).</summary>
public enum DependenciaAdministrativa
{
    /// <summary>Dependencia administrativa federal.</summary>
    Federal = 1,

    /// <summary>Dependencia administrativa estadual.</summary>
    Estadual = 2,

    /// <summary>Dependencia administrativa municipal.</summary>
    Municipal = 3,

    /// <summary>Dependencia administrativa privada.</summary>
    Privada = 4,
}

/// <summary>Situacao (estado) de uma escola no ciclo de operacao da rede de ensino.</summary>
public enum SituacaoEscola
{
    /// <summary>Em cadastro (ainda nao credenciada).</summary>
    EmCadastro = 1,

    /// <summary>Credenciada e apta a operar.</summary>
    Credenciada = 2,

    /// <summary>Desativada (terminal).</summary>
    Desativada = 3,
}
