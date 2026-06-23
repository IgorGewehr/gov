using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Common;
using Tensorroot.Gov.Modules.Saude.Domain.Estabelecimentos;

namespace Tensorroot.Gov.Modules.Saude.Application.Estabelecimentos;

/// <summary>
/// Lista/busca paginada de estabelecimentos por nome/CNES, com filtros por tipo e situacao
/// (navegabilidade). Tenant-scoped via Global Query Filter; read-only.
/// </summary>
/// <param name="Termo">Termo livre (nome ou CNES); nulo lista tudo.</param>
/// <param name="Tipo">Filtro opcional por tipo.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarEstabelecimentosQuery(
    string? Termo,
    TipoEstabelecimento? Tipo,
    SituacaoEstabelecimento? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<EstabelecimentoItemLista>>;

/// <summary>Handler da busca paginada de estabelecimentos.</summary>
public sealed class BuscarEstabelecimentosHandler(IEstabelecimentoCadastroRepository estabelecimentos)
    : IQueryHandler<BuscarEstabelecimentosQuery, ResultadoPaginado<EstabelecimentoItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<EstabelecimentoItemLista>> Handle(
        BuscarEstabelecimentosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await estabelecimentos
            .BuscarAsync(request.Termo, request.Tipo, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(estabelecimento => new EstabelecimentoItemLista(
                estabelecimento.Id.Value,
                estabelecimento.Cnes.Valor,
                estabelecimento.Nome,
                estabelecimento.Tipo.ToString(),
                estabelecimento.Endereco.Municipio,
                estabelecimento.Endereco.Uf,
                estabelecimento.Situacao.ToString()))
            .ToList();

        return new ResultadoPaginado<EstabelecimentoItemLista>(projetados, total, pagina, tamanho);
    }
}
