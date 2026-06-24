namespace Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;

/// <summary>Natureza do item de catalogo: material/produto (CATMAT) ou servico (CATSER).</summary>
public enum NaturezaItem
{
    /// <summary>Material/produto padronizado (CATMAT).</summary>
    Material = 1,

    /// <summary>Servico padronizado (CATSER).</summary>
    Servico = 2,
}

/// <summary>Situacao cadastral do item no catalogo.</summary>
public enum SituacaoItemCatalogo
{
    /// <summary>Item disponivel para uso em compras/contratacoes.</summary>
    Ativo = 1,

    /// <summary>Item descontinuado: nao pode ser usado em novas contratacoes.</summary>
    Inativo = 2,
}
