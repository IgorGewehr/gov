namespace Tensorroot.Gov.Modules.Administracao.Domain.Pca;

/// <summary>Situacao do Plano de Contratacoes Anual (PCA) no seu ciclo (art. 12, VII, Lei 14.133/2021).</summary>
public enum SituacaoPca
{
    /// <summary>Em elaboracao: itens podem ser incluidos/alterados/removidos.</summary>
    EmElaboracao = 1,

    /// <summary>Aprovado pela autoridade competente: itens congelados (alteracao por revisao formal).</summary>
    Aprovado = 2,

    /// <summary>Publicado no PNCP (art. 12, par. 1º): vigente para o exercicio.</summary>
    Publicado = 3,

    /// <summary>
    /// Em revisao: o plano vigente foi reaberto para alteracao formal (inclusao/exclusao/ajuste de itens)
    /// apos aprovacao/publicacao. Concluida a revisao, retorna a Aprovado para reaprovacao/republicacao.
    /// </summary>
    EmRevisao = 4,
}

/// <summary>
/// Natureza da contratacao gerada a partir de um item do PCA, para rastrear o vinculo entre o
/// planejamento (art. 12, VII) e o instrumento que o concretizou (licitacao, ata de registro de precos,
/// dispensa, inexigibilidade/credenciamento).
/// </summary>
public enum FonteContratacaoPca
{
    /// <summary>Licitacao (qualquer modalidade da Lei 14.133/2021).</summary>
    Licitacao = 1,

    /// <summary>Ata de Registro de Precos (ARP — art. 82-86).</summary>
    AtaRegistroPrecos = 2,

    /// <summary>Dispensa de licitacao (art. 75).</summary>
    Dispensa = 3,

    /// <summary>Inexigibilidade de licitacao, inclusive credenciamento (art. 74, IV c/c art. 79).</summary>
    Inexigibilidade = 4,
}
