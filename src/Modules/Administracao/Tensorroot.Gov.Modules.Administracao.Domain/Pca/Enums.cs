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
}
