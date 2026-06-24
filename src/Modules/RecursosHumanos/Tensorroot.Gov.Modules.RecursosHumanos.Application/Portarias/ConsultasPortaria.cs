using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Common;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Portarias;

/// <summary>Busca paginada de portarias por tipo/situacao/exercicio/servidor (navegabilidade); read-only.</summary>
/// <param name="Tipo">Filtro opcional por natureza do ato.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Exercicio">Filtro opcional por exercicio.</param>
/// <param name="ServidorId">Filtro opcional por servidor vinculado.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarPortariasQuery(
    TipoPortaria? Tipo,
    SituacaoPortaria? Situacao,
    int? Exercicio,
    Guid? ServidorId,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<PortariaResumo>>;

/// <summary>Handler da busca paginada de portarias.</summary>
public sealed class BuscarPortariasHandler(IPortariaRepository portarias)
    : IQueryHandler<BuscarPortariasQuery, ResultadoPaginado<PortariaResumo>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<PortariaResumo>> Handle(BuscarPortariasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await portarias
            .BuscarAsync(request.Tipo, request.Situacao, request.Exercicio, request.ServidorId, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens.Select(ProjetarPortaria.ParaResumo).ToList();
        return new ResultadoPaginado<PortariaResumo>(projetados, total, pagina, tamanho);
    }
}

/// <summary>Obtem uma portaria por identificador (detalhe completo com texto integral); read-only.</summary>
/// <param name="PortariaId">Identificador da portaria.</param>
public sealed record ObterPortariaQuery(Guid PortariaId) : IQuery<PortariaDetalhe>;

/// <summary>Handler do detalhe de portaria.</summary>
public sealed class ObterPortariaHandler(IPortariaRepository portarias)
    : IQueryHandler<ObterPortariaQuery, PortariaDetalhe>
{
    /// <inheritdoc />
    public async Task<PortariaDetalhe> Handle(ObterPortariaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var portaria = await portarias.ObterPorIdAsync(new PortariaId(request.PortariaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Portaria nao encontrada.");
        return ProjetarPortaria.ParaDetalhe(portaria);
    }
}
