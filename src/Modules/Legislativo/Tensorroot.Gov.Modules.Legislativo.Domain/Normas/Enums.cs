namespace Tensorroot.Gov.Modules.Legislativo.Domain.Normas;

/// <summary>Especie da norma juridica municipal (CF/88 art. 59 e Lei Organica Municipal).</summary>
public enum TipoNorma
{
    /// <summary>Lei ordinaria.</summary>
    Lei = 1,

    /// <summary>Lei complementar (quorum qualificado; materias reservadas pela LOM).</summary>
    LeiComplementar = 2,

    /// <summary>Decreto legislativo (competencia exclusiva da Camara; nao depende de sancao).</summary>
    DecretoLegislativo = 3,

    /// <summary>Resolucao (materia de economia interna/regimental da Camara).</summary>
    Resolucao = 4,

    /// <summary>Emenda a Lei Organica Municipal.</summary>
    EmendaLOM = 5,

    /// <summary>Lei Organica do Municipio (norma fundamental).</summary>
    LeiOrganica = 6,
}

/// <summary>Ciclo de vigencia de uma norma juridica.</summary>
public enum SituacaoVigencia
{
    /// <summary>Em vigor (eficaz).</summary>
    EmVigor = 1,

    /// <summary>Alterada por norma posterior (continua em vigor com a alteracao) — nao terminal.</summary>
    Alterada = 2,

    /// <summary>Revogada (terminal — nao admite nova revogacao).</summary>
    Revogada = 3,
}

/// <summary>Tipo do evento registrado na trilha imutavel de vigencia de uma norma.</summary>
public enum TipoEventoVigencia
{
    /// <summary>Promulgacao/publicacao original (nascimento da norma em vigor).</summary>
    Promulgacao = 1,

    /// <summary>Alteracao por norma posterior.</summary>
    Alteracao = 2,

    /// <summary>Revogacao por norma posterior (ou expressa).</summary>
    Revogacao = 3,
}
