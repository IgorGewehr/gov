using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Catalogo;

/// <summary>Resumo de um item de catalogo para listagem.</summary>
/// <param name="Id">Identificador do item.</param>
/// <param name="Codigo">Codigo padronizado.</param>
/// <param name="Natureza">Material ou servico.</param>
/// <param name="Descricao">Descricao padronizada.</param>
/// <param name="UnidadeFornecimento">Unidade de fornecimento.</param>
/// <param name="Classe">Classe/classificacao.</param>
/// <param name="Situacao">Situacao cadastral.</param>
public sealed record ItemCatalogoResumo(
    Guid Id,
    string Codigo,
    string Natureza,
    string Descricao,
    string UnidadeFornecimento,
    string? Classe,
    string Situacao);

/// <summary>Lista itens do catalogo com filtros opcionais (tenant-scoped).</summary>
/// <param name="Natureza">Natureza a filtrar (opcional).</param>
/// <param name="Termo">Termo de busca em codigo/descricao (opcional).</param>
/// <param name="ApenasAtivos">Quando verdadeiro, retorna apenas itens Ativos.</param>
public sealed record ListarItensCatalogoQuery(NaturezaItem? Natureza, string? Termo, bool ApenasAtivos)
    : IQuery<IReadOnlyList<ItemCatalogoResumo>>;

/// <summary>Handler da listagem de itens de catalogo.</summary>
public sealed class ListarItensCatalogoHandler(ICatalogoRepository catalogo)
    : IQueryHandler<ListarItensCatalogoQuery, IReadOnlyList<ItemCatalogoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemCatalogoResumo>> Handle(
        ListarItensCatalogoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await catalogo
            .ListarAsync(request.Natureza, request.Termo, request.ApenasAtivos, cancellationToken)
            .ConfigureAwait(false);

        return itens
            .Select(i => new ItemCatalogoResumo(
                i.Id.Value,
                i.Codigo,
                i.Natureza.ToString(),
                i.Descricao,
                i.UnidadeFornecimento,
                i.Classe,
                i.Situacao.ToString()))
            .ToList();
    }
}
