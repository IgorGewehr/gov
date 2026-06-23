namespace Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;

/// <summary>Situacao cadastral do estabelecimento de saude (CNES).</summary>
public enum SituacaoEstabelecimento
{
    /// <summary>Estabelecimento ativo (estado inicial apos o cadastro).</summary>
    Ativo = 1,

    /// <summary>Estabelecimento inativo (encerrado/suspenso) — nao recebe novos atendimentos.</summary>
    Inativo = 2,
}

/// <summary>
/// Tipo do estabelecimento de saude (subconjunto operacional da tabela de tipos do CNES).
/// </summary>
public enum TipoEstabelecimento
{
    /// <summary>Unidade Basica de Saude.</summary>
    Ubs = 1,

    /// <summary>Unidade de Pronto Atendimento.</summary>
    Upa = 2,

    /// <summary>Hospital.</summary>
    Hospital = 3,

    /// <summary>Centro de Atencao Psicossocial.</summary>
    Caps = 4,

    /// <summary>Farmacia / unidade de dispensacao.</summary>
    Farmacia = 5,

    /// <summary>Unidade de Saude da Familia.</summary>
    UnidadeSaudeFamilia = 6,

    /// <summary>Centro de Especialidades / policlinica.</summary>
    CentroEspecialidades = 7,

    /// <summary>Laboratorio.</summary>
    Laboratorio = 8,

    /// <summary>Outro tipo de estabelecimento.</summary>
    Outro = 99,
}
