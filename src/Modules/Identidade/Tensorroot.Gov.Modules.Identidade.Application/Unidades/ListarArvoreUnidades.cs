using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

namespace Tensorroot.Gov.Modules.Identidade.Application.Unidades;

/// <summary>No da arvore de UOs (projecao de leitura, com filhos aninhados).</summary>
/// <param name="Id">Identificador da UO.</param>
/// <param name="Codigo">Codigo estavel.</param>
/// <param name="Nome">Nome de exibicao.</param>
/// <param name="Tipo">Natureza administrativa.</param>
/// <param name="Ativa">Se a UO esta ativa.</param>
/// <param name="Filhos">Subunidades diretas (recursivo).</param>
public sealed record NoUnidade(
    Guid Id,
    string Codigo,
    string Nome,
    TipoUnidade Tipo,
    bool Ativa,
    IReadOnlyList<NoUnidade> Filhos);

/// <summary>Lista a arvore de UOs do tenant atual (raizes com filhos aninhados).</summary>
public sealed record ListarArvoreUnidadesQuery : IQuery<IReadOnlyList<NoUnidade>>;

/// <summary>Handler da listagem da arvore de UOs.</summary>
public sealed class ListarArvoreUnidadesHandler(IUnidadeRepository unidades)
    : IQueryHandler<ListarArvoreUnidadesQuery, IReadOnlyList<NoUnidade>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<NoUnidade>> Handle(ListarArvoreUnidadesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var todas = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);

        var filhosPorPai = todas
            .Where(unidade => unidade.UnidadePaiId is not null)
            .GroupBy(unidade => unidade.UnidadePaiId!.Value)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());

        var raizes = todas.Where(unidade => unidade.UnidadePaiId is null).ToList();

        return raizes
            .OrderBy(unidade => unidade.Codigo, StringComparer.Ordinal)
            .Select(raiz => Montar(raiz, filhosPorPai))
            .ToList();
    }

    private static NoUnidade Montar(
        UnidadeOrganizacional unidade,
        IReadOnlyDictionary<UnidadeOrganizacionalId, List<UnidadeOrganizacional>> filhosPorPai)
    {
        var filhos = filhosPorPai.TryGetValue(unidade.Id, out var lista)
            ? lista
                .OrderBy(filho => filho.Codigo, StringComparer.Ordinal)
                .Select(filho => Montar(filho, filhosPorPai))
                .ToList()
            : [];

        return new NoUnidade(
            unidade.Id.Value,
            unidade.Codigo,
            unidade.Nome,
            unidade.Tipo,
            unidade.Ativa,
            filhos);
    }
}
