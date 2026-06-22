namespace Tensorroot.Gov.Modules.Legislativo.Domain.Comissoes;

/// <summary>Natureza da comissao (Regimento Interno; CF/88 art. 58).</summary>
public enum TipoComissao
{
    /// <summary>Permanente (ex.: CCJ, Financas e Orcamento) — subsiste entre legislaturas/sessoes.</summary>
    Permanente = 1,

    /// <summary>Temporaria (especial/CPI/representacao) — extingue-se ao fim do objeto/prazo.</summary>
    Temporaria = 2,
}

/// <summary>Situacao (estado) da comissao.</summary>
public enum SituacaoComissao
{
    /// <summary>Ativa (em funcionamento).</summary>
    Ativa = 1,

    /// <summary>Extinta (terminal — temporaria encerrada).</summary>
    Extinta = 2,
}

/// <summary>Papel de um membro na comissao.</summary>
public enum PapelMembro
{
    /// <summary>Membro efetivo (titular).</summary>
    Efetivo = 1,

    /// <summary>Suplente.</summary>
    Suplente = 2,
}

/// <summary>Cargo de direcao na comissao.</summary>
public enum CargoComissao
{
    /// <summary>Sem cargo de direcao (membro comum).</summary>
    Nenhum = 0,

    /// <summary>Presidente da comissao.</summary>
    Presidente = 1,

    /// <summary>Vice-Presidente.</summary>
    VicePresidente = 2,

    /// <summary>Relator.</summary>
    Relator = 3,
}
