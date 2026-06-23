namespace Tensorroot.Gov.Modules.Saude.Domain.Profissionais;

/// <summary>Situacao cadastral do profissional de saude.</summary>
public enum SituacaoProfissional
{
    /// <summary>Profissional ativo (estado inicial apos o cadastro).</summary>
    Ativo = 1,

    /// <summary>Profissional inativo (desligado) — nao recebe novos vinculos.</summary>
    Inativo = 2,
}

/// <summary>Conselho de classe que habilita o profissional de saude (subconjunto operacional).</summary>
public enum TipoConselho
{
    /// <summary>Conselho Regional de Medicina.</summary>
    Crm = 1,

    /// <summary>Conselho Regional de Enfermagem.</summary>
    Coren = 2,

    /// <summary>Conselho Regional de Odontologia.</summary>
    Cro = 3,

    /// <summary>Conselho Regional de Farmacia.</summary>
    Crf = 4,

    /// <summary>Conselho Regional de Psicologia.</summary>
    Crp = 5,

    /// <summary>Conselho Regional de Fisioterapia e Terapia Ocupacional.</summary>
    Crefito = 6,

    /// <summary>Conselho Regional de Nutricionistas.</summary>
    Crn = 7,

    /// <summary>Conselho Regional de Servico Social.</summary>
    Cress = 8,

    /// <summary>Outro conselho de classe.</summary>
    Outro = 99,
}
