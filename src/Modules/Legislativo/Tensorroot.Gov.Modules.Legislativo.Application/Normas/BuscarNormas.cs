using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Normas;

/// <summary>Resumo de uma norma para listagem.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Tipo">Especie.</param>
/// <param name="Numero">Numero.</param>
/// <param name="Ano">Ano.</param>
/// <param name="Ementa">Resumo do objeto.</param>
/// <param name="DataPromulgacao">Data de promulgacao.</param>
/// <param name="Situacao">Situacao de vigencia.</param>
public sealed record NormaResumo(
    Guid Id,
    string Tipo,
    int Numero,
    int Ano,
    string Ementa,
    DateOnly DataPromulgacao,
    string Situacao);

/// <summary>Pagina de resultados de busca de normas.</summary>
/// <param name="Itens">Itens da pagina.</param>
/// <param name="Total">Total de itens (todas as paginas).</param>
/// <param name="Pagina">Pagina atual (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record PaginaNormas(IReadOnlyList<NormaResumo> Itens, int Total, int Pagina, int Tamanho);

/// <summary>Busca filtrada/paginada de normas do tenant (por termo/tipo/numero/ano/situacao).</summary>
/// <param name="Termo">Termo livre na ementa (opcional).</param>
/// <param name="Tipo">Tipo (<see cref="TipoNorma"/>) opcional.</param>
/// <param name="Numero">Numero exato (opcional).</param>
/// <param name="Ano">Ano exato (opcional).</param>
/// <param name="Situacao">Situacao (<see cref="SituacaoVigencia"/>) opcional.</param>
/// <param name="Pagina">Pagina (base 1; default 1).</param>
/// <param name="Tamanho">Tamanho da pagina (default 20).</param>
public sealed record BuscarNormasQuery(
    string? Termo = null,
    int? Tipo = null,
    int? Numero = null,
    int? Ano = null,
    int? Situacao = null,
    int Pagina = 1,
    int Tamanho = 20) : IQuery<PaginaNormas>;

/// <summary>Handler da busca de normas.</summary>
public sealed class BuscarNormasHandler(INormaRepository normas)
    : IQueryHandler<BuscarNormasQuery, PaginaNormas>
{
    /// <inheritdoc />
    public async Task<PaginaNormas> Handle(BuscarNormasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pagina = request.Pagina < 1 ? 1 : request.Pagina;
        var tamanho = request.Tamanho is < 1 or > 200 ? 20 : request.Tamanho;

        var filtro = new FiltroNormas(
            string.IsNullOrWhiteSpace(request.Termo) ? null : request.Termo.Trim(),
            request.Tipo is { } t && Enum.IsDefined(typeof(TipoNorma), t) ? (TipoNorma)t : null,
            request.Numero,
            request.Ano,
            request.Situacao is { } s && Enum.IsDefined(typeof(SituacaoVigencia), s) ? (SituacaoVigencia)s : null,
            pagina,
            tamanho);

        var (itens, total) = await normas.BuscarAsync(filtro, cancellationToken).ConfigureAwait(false);

        var resumos = itens
            .Select(norma => new NormaResumo(
                norma.Id.Value,
                norma.Tipo.ToString(),
                norma.Numero,
                norma.Ano,
                norma.Ementa.Valor,
                norma.DataPromulgacao,
                norma.SituacaoVigencia.ToString()))
            .ToList();

        return new PaginaNormas(resumos, total, pagina, tamanho);
    }
}
