namespace Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;

/// <summary>Situacao da edicao do Diario Oficial Eletronico.</summary>
public enum SituacaoEdicao
{
    /// <summary>Rascunho (em montagem; admite materias).</summary>
    Rascunho = 1,

    /// <summary>Publicada (imutavel; marco legal de eficacia dos atos).</summary>
    Publicada = 2,
}

/// <summary>Especie da materia publicada em uma edicao do Diario.</summary>
public enum TipoMateria
{
    /// <summary>Norma juridica (lei, decreto, resolucao).</summary>
    Norma = 1,

    /// <summary>Ata de sessao plenaria.</summary>
    AtaSessao = 2,

    /// <summary>Edital.</summary>
    Edital = 3,

    /// <summary>Portaria.</summary>
    Portaria = 4,

    /// <summary>Extrato (contrato, ata de registro de precos, etc.).</summary>
    Extrato = 5,

    /// <summary>Outro ato.</summary>
    Outro = 6,
}
