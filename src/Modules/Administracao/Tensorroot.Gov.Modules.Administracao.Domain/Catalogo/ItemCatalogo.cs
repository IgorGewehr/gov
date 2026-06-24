using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;

/// <summary>Identificador forte do agregado <see cref="ItemCatalogo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemCatalogoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemCatalogoId"/>.</returns>
    public static ItemCatalogoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Item padronizado do catalogo de materiais e servicos (CATMAT/CATSER), base estruturada das
/// compras e contratacoes (Lei 14.133/2021). Substitui a descricao livre por um item identificavel,
/// com unidade de fornecimento e classe/classificacao, evitando duplicidade e permitindo comparacao
/// de precos. Raiz de agregado, isolada por tenant.
/// </summary>
public sealed class ItemCatalogo : AggregateRoot<ItemCatalogoId>, IMustHaveTenant
{
    private ItemCatalogo()
    {
    }

    private ItemCatalogo(
        ItemCatalogoId id,
        Guid tenantId,
        string codigo,
        NaturezaItem natureza,
        string descricao,
        string unidadeFornecimento,
        string? classe)
        : base(id)
    {
        TenantId = tenantId;
        Codigo = codigo;
        Natureza = natureza;
        Descricao = descricao;
        UnidadeFornecimento = unidadeFornecimento;
        Classe = classe;
        Situacao = SituacaoItemCatalogo.Ativo;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Codigo padronizado do item (unico por tenant) — ex.: codigo CATMAT/CATSER ou interno.</summary>
    public string Codigo { get; private set; } = default!;

    /// <summary>Natureza do item: material (CATMAT) ou servico (CATSER).</summary>
    public NaturezaItem Natureza { get; private set; }

    /// <summary>Descricao padronizada do item.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Unidade de fornecimento (ex.: UN, KG, M, HORA, MES).</summary>
    public string UnidadeFornecimento { get; private set; } = default!;

    /// <summary>Classe/classificacao (grupo CATMAT/CATSER ou classe interna), opcional.</summary>
    public string? Classe { get; private set; }

    /// <summary>Situacao cadastral atual.</summary>
    public SituacaoItemCatalogo Situacao { get; private set; }

    /// <summary>
    /// Cadastra um novo item de catalogo (nasce <see cref="SituacaoItemCatalogo.Ativo"/>).
    /// A unicidade de <c>(TenantId, Codigo)</c> e garantida pelo handler/indice.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="codigo">Codigo padronizado (obrigatorio).</param>
    /// <param name="natureza">Material ou servico.</param>
    /// <param name="descricao">Descricao padronizada (obrigatoria).</param>
    /// <param name="unidadeFornecimento">Unidade de fornecimento (obrigatoria).</param>
    /// <param name="classe">Classe/classificacao (opcional).</param>
    /// <returns>Novo <see cref="ItemCatalogo"/>.</returns>
    /// <exception cref="ArgumentException">Se codigo, descricao ou unidade forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a natureza nao for valor valido do enum.</exception>
    public static ItemCatalogo Cadastrar(
        Guid tenantId,
        string codigo,
        NaturezaItem natureza,
        string descricao,
        string unidadeFornecimento,
        string? classe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeFornecimento);
        if (!Enum.IsDefined(natureza))
        {
            throw new ArgumentOutOfRangeException(nameof(natureza), "Natureza do item invalida.");
        }

        return new ItemCatalogo(
            ItemCatalogoId.New(),
            tenantId,
            codigo.Trim(),
            natureza,
            descricao.Trim(),
            unidadeFornecimento.Trim(),
            string.IsNullOrWhiteSpace(classe) ? null : classe.Trim());
    }

    /// <summary>Atualiza dados descritivos do item (mantem codigo e natureza).</summary>
    /// <param name="descricao">Nova descricao (obrigatoria).</param>
    /// <param name="unidadeFornecimento">Nova unidade de fornecimento (obrigatoria).</param>
    /// <param name="classe">Nova classe/classificacao (opcional).</param>
    /// <exception cref="ArgumentException">Se descricao ou unidade forem vazias.</exception>
    /// <exception cref="InvalidOperationException">Se o item estiver Inativo.</exception>
    public void Atualizar(string descricao, string unidadeFornecimento, string? classe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeFornecimento);
        if (Situacao == SituacaoItemCatalogo.Inativo)
        {
            throw new InvalidOperationException("Item inativo nao pode ser atualizado; reative-o antes.");
        }

        Descricao = descricao.Trim();
        UnidadeFornecimento = unidadeFornecimento.Trim();
        Classe = string.IsNullOrWhiteSpace(classe) ? null : classe.Trim();
    }

    /// <summary>Inativa o item (impede uso em novas contratacoes).</summary>
    /// <exception cref="InvalidOperationException">Se o item ja estiver Inativo.</exception>
    public void Inativar()
    {
        if (Situacao == SituacaoItemCatalogo.Inativo)
        {
            throw new InvalidOperationException("Item ja esta Inativo.");
        }

        Situacao = SituacaoItemCatalogo.Inativo;
    }

    /// <summary>Reativa um item previamente inativado.</summary>
    /// <exception cref="InvalidOperationException">Se o item ja estiver Ativo.</exception>
    public void Reativar()
    {
        if (Situacao == SituacaoItemCatalogo.Ativo)
        {
            throw new InvalidOperationException("Item ja esta Ativo.");
        }

        Situacao = SituacaoItemCatalogo.Ativo;
    }
}
