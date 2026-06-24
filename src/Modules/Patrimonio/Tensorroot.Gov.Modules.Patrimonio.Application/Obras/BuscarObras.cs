using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Application.Common;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>Linha de listagem de obra (navegabilidade — Onda 0).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Objeto">Objeto da obra.</param>
/// <param name="Municipio">Município.</param>
/// <param name="Uf">UF.</param>
/// <param name="ValorContratado">Valor contratado.</param>
/// <param name="PercentualFisicoAcumulado">Percentual físico acumulado.</param>
/// <param name="Situacao">Situação.</param>
public sealed record ObraLinha(
    Guid Id,
    string Objeto,
    string Municipio,
    string Uf,
    decimal ValorContratado,
    decimal PercentualFisicoAcumulado,
    string Situacao);

/// <summary>Busca paginada de obras por objeto/município, com filtro opcional por situação.</summary>
/// <param name="Termo">Termo livre (objeto ou município).</param>
/// <param name="Situacao">Filtro por situação.</param>
/// <param name="Pagina">Página (base 1).</param>
/// <param name="Tamanho">Tamanho da página.</param>
public sealed record BuscarObrasQuery(string? Termo, SituacaoObra? Situacao, int? Pagina, int? Tamanho)
    : IQuery<ResultadoPaginado<ObraLinha>>;

/// <summary>Handler da busca de obras.</summary>
public sealed class BuscarObrasHandler(IObraRepository obras) : IQueryHandler<BuscarObrasQuery, ResultadoPaginado<ObraLinha>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<ObraLinha>> Handle(BuscarObrasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await obras
            .BuscarAsync(request.Termo, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var linhas = itens
            .Select(obra => new ObraLinha(
                obra.Id.Value,
                obra.Objeto,
                obra.Localizacao.Municipio,
                obra.Localizacao.Uf,
                obra.ValorContratado.Valor,
                obra.PercentualFisicoAcumulado,
                obra.Situacao.ToString()))
            .ToList();

        return new ResultadoPaginado<ObraLinha>(linhas, total, pagina, tamanho);
    }
}
