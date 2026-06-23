using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Common;

namespace Tensorroot.Gov.Modules.Saude.Application.Farmacia;

/// <summary>
/// Lista/busca paginada do catalogo de medicamentos por principio ativo/apresentacao. Dado NAO
/// sensivel (catalogo) — nao gera trilha LGPD. Tenant-scoped via Global Query Filter.
/// </summary>
/// <param name="Termo">Termo livre (principio ativo/apresentacao); nulo lista tudo.</param>
/// <param name="ApenasAtivos">Quando verdadeiro, filtra apenas itens ativos.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarMedicamentosQuery(
    string? Termo,
    bool ApenasAtivos,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<MedicamentoItemLista>>;

/// <summary>Handler da busca do catalogo de medicamentos.</summary>
public sealed class BuscarMedicamentosHandler(IMedicamentoRepository medicamentos)
    : IQueryHandler<BuscarMedicamentosQuery, ResultadoPaginado<MedicamentoItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<MedicamentoItemLista>> Handle(
        BuscarMedicamentosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await medicamentos
            .BuscarAsync(request.Termo, request.ApenasAtivos, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(m => new MedicamentoItemLista(
                m.Id.Value,
                m.PrincipioAtivo,
                m.Apresentacao,
                m.Concentracao,
                m.Forma.ToString(),
                m.Unidade.ToString(),
                m.Controle.ToString(),
                m.ExigeReceitaControlada,
                m.Ativo))
            .ToList();

        return new ResultadoPaginado<MedicamentoItemLista>(projetados, total, pagina, tamanho);
    }
}
