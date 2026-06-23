using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Application.Common;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Item da lista de veiculos (navegabilidade — Onda 0): projecao enxuta para a tabela.</summary>
/// <param name="Id">Identificador do veiculo.</param>
/// <param name="Placa">Placa (CTB).</param>
/// <param name="Renavam">RENAVAM.</param>
/// <param name="Descricao">Descricao do veiculo.</param>
/// <param name="NumeroTombamento">Numero de tombo, se tombado.</param>
/// <param name="Odometro">Quilometragem atual.</param>
/// <param name="ValorContabil">Valor contabil atual.</param>
/// <param name="Situacao">Situacao no ciclo patrimonial.</param>
public sealed record VeiculoItemLista(
    Guid Id,
    string Placa,
    string Renavam,
    string Descricao,
    string? NumeroTombamento,
    int Odometro,
    decimal ValorContabil,
    string Situacao);

/// <summary>
/// Lista/busca paginada de veiculos da frota (navegabilidade — Onda 0). Tenant-scoped via Global
/// Query Filter; read-only. Filtra por termo livre (descricao/placa/RENAVAM) e, opcionalmente, situacao.
/// </summary>
/// <param name="Termo">Termo livre (descricao, placa ou RENAVAM); nulo lista tudo.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarVeiculosQuery(
    string? Termo,
    SituacaoBemPatrimonial? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<VeiculoItemLista>>;

/// <summary>Handler da busca paginada de veiculos.</summary>
public sealed class BuscarVeiculosHandler(IVeiculoRepository veiculos)
    : IQueryHandler<BuscarVeiculosQuery, ResultadoPaginado<VeiculoItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<VeiculoItemLista>> Handle(
        BuscarVeiculosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await veiculos
            .BuscarAsync(request.Termo, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(veiculo => new VeiculoItemLista(
                veiculo.Id.Value,
                veiculo.Placa.Valor,
                veiculo.Renavam.Digitos,
                veiculo.Descricao,
                veiculo.NumeroTombamento,
                veiculo.Odometro.Valor,
                veiculo.ValorContabil.Valor,
                veiculo.Situacao.ToString()))
            .ToList();

        return new ResultadoPaginado<VeiculoItemLista>(projetados, total, pagina, tamanho);
    }
}
