using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Common;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>
/// Lista/busca paginada de servidores por nome/matricula (navegabilidade — Onda 0), com filtros
/// opcionais por situacao, regime e cargo. Tenant-scoped via Global Query Filter; read-only.
/// CPF mascarado na projecao (LGPD).
/// </summary>
/// <param name="Termo">Termo livre (nome ou matricula); nulo lista tudo.</param>
/// <param name="Situacao">Filtro opcional por situacao do vinculo.</param>
/// <param name="Regime">Filtro opcional por regime previdenciario.</param>
/// <param name="CargoId">Filtro opcional por cargo provido.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarServidoresQuery(
    string? Termo,
    SituacaoServidor? Situacao,
    RegimePrevidenciario? Regime,
    Guid? CargoId,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<ServidorResumo>>;

/// <summary>Handler da busca paginada de servidores.</summary>
public sealed class BuscarServidoresHandler(IServidorRepository servidores)
    : IQueryHandler<BuscarServidoresQuery, ResultadoPaginado<ServidorResumo>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<ServidorResumo>> Handle(
        BuscarServidoresQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);
        var cargoId = request.CargoId is { } id ? new CargoId(id) : (CargoId?)null;

        var (itens, total) = await servidores
            .BuscarAsync(request.Termo, request.Situacao, request.Regime, cargoId, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(ProjetarServidor.ParaResumo)
            .ToList();

        return new ResultadoPaginado<ServidorResumo>(projetados, total, pagina, tamanho);
    }
}
